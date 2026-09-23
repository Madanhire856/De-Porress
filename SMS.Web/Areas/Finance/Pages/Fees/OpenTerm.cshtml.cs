using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class OpenTermModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public OpenTermModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? TermId { get; set; }

        public List<SelectListItem> Terms { get; set; } = new();
        public TermInfo? SelectedTerm { get; set; }
        public string BaseCurrency { get; set; } = "USD";

        // Counts for the summary tiles
        public int StudentsWithLedgerCount { get; set; }
        public int StudentsWithoutLedgerCount { get; set; }
        public decimal ProjectedCharged { get; set; }

        // Grouped preview
        public List<PreviewRow> Preview { get; set; } = new();

        public class TermInfo
        {
            public Guid Id { get; set; }
            public string Display { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class PreviewRow
        {
            public Guid GradeId { get; set; }
            public string GradeName { get; set; } = "";
            public int StudentCount { get; set; }
            public decimal ChargePerStudent { get; set; }
            public decimal TotalCharge { get; set; }
        }

        // =============================================================
        //  GET — preview
        // =============================================================
        public async Task<IActionResult> OnGetAsync()
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            await LoadAsync();
            return Page();
        }

        // =============================================================
        //  POST — actually open the term
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            if (!TermId.HasValue)
            {
                TempData["OpenTermError"] = "Select a term.";
                await LoadAsync();
                return Page();
            }

            // Re-verify the term still exists
            var term = await _context.Terms
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == TermId.Value);

            if (term == null)
            {
                TempData["OpenTermError"] = "That term no longer exists.";
                await LoadAsync();
                return Page();
            }

            // Recompute the plan from scratch (user may have POST'd stale data)
            var plan = await BuildPlanAsync(TermId.Value);

            if (!plan.NewStudents.Any())
            {
                TempData["OpenTermError"] =
                    "No new students to open ledgers for in this term.";
                return RedirectToPage("./OpenTerm", new { TermId });
            }

            var baseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            var now = DateTime.Now;
            var creatorId = _currentUser.UserId ?? Guid.Empty;

            foreach (var entry in plan.NewStudents)
            {
                var opening = entry.Charge;
                var statusId = opening <= 0m
                    ? (int)StudentLedgerStatus.SETTLED
                    : (int)StudentLedgerStatus.OVER_DUE;

                _context.StudentLedgers.Add(new StudentLedger
                {
                    Id = Guid.NewGuid(),
                    StudentId = entry.StudentId,
                    TermId = TermId.Value,
                    OpeningBalance = opening,
                    ClosingBalance = opening,
                    CurrencyId = baseCurrency,
                    StatusId = statusId,
                    CreatorId = creatorId,
                    CreationDate = now
                });
            }

            await _context.SaveChangesAsync();

            var count = plan.NewStudents.Count;
            TempData["OpenTermSuccess"] =
                $"Opened {count} ledger{(count == 1 ? "" : "s")} for {term.Name ?? $"{term.AcademicYear} · Term {term.Number}"}.";

            return RedirectToPage("./Balances", new { TermId });
        }

        // =============================================================
        //  LOAD
        // =============================================================
        private async Task LoadAsync()
        {
            var today = DateTime.Today;

            var termRows = await _context.Terms.AsNoTracking()
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            Terms = termRows.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(t.Name)
                    ? $"{t.AcademicYear} · Term {t.Number}"
                    : t.Name
            }).ToList();

            // Default to the active term, or the most recent
            if (!TermId.HasValue)
            {
                var active = termRows.FirstOrDefault(t => t.StartDate <= today && t.EndDate >= today);
                TermId = active?.Id ?? termRows.FirstOrDefault()?.Id;
            }

            var selected = termRows.FirstOrDefault(t => t.Id == TermId);
            if (selected != null)
            {
                SelectedTerm = new TermInfo
                {
                    Id = selected.Id,
                    Display = string.IsNullOrWhiteSpace(selected.Name)
                        ? $"{selected.AcademicYear} · Term {selected.Number}"
                        : selected.Name,
                    StartDate = selected.StartDate,
                    EndDate = selected.EndDate,
                    IsActive = selected.StartDate <= today && selected.EndDate >= today
                };
            }

            BaseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            if (!TermId.HasValue) return;

            var plan = await BuildPlanAsync(TermId.Value);

            StudentsWithLedgerCount = plan.ExistingLedgerCount;
            StudentsWithoutLedgerCount = plan.NewStudents.Count;

            Preview = plan.Preview;
            ProjectedCharged = Preview.Sum(p => p.TotalCharge);
        }

        // =============================================================
        //  BUILD PLAN — shared between GET and POST
        // =============================================================
        private async Task<PlanResult> BuildPlanAsync(Guid termId)
        {
            var existingStudentIds = await _context.StudentLedgers.AsNoTracking()
                .Where(l => l.TermId == termId)
                .Select(l => l.StudentId)
                .ToListAsync();

            var existingSet = existingStudentIds.ToHashSet();

            var activeStudents = await _context.Students.AsNoTracking()
                .Where(s => s.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE)
                .ToListAsync();

            var newStudents = activeStudents
                .Where(s => !existingSet.Contains(s.Id))
                .ToList();

            // Charge lookup: grade -> sum of fees for that term
            var chargeByGrade = await _context.FeesStructures.AsNoTracking()
                .Where(f => f.TermId == termId)
                .GroupBy(f => f.GradeId)
                .Select(g => new
                {
                    GradeId = g.Key,
                    Total = g.Sum(f => f.Amount)
                })
                .ToDictionaryAsync(x => x.GradeId, x => x.Total);

            // Class -> Grade mapping
            var classToGrade = await _context.Classes.AsNoTracking()
                .Select(c => new { c.Id, c.GradeId })
                .ToDictionaryAsync(x => x.Id, x => x.GradeId);

            // Grade names
            var gradeNames = await _context.Grades.AsNoTracking()
                .Select(g => new { g.Id, g.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            // Per-student plan
            var plans = newStudents.Select(s =>
            {
                Guid? gradeId = null;
                if (s.ClassId.HasValue
                    && classToGrade.TryGetValue(s.ClassId.Value, out var gid))
                {
                    gradeId = gid;
                }

                decimal charge = 0m;
                if (gradeId.HasValue
                    && chargeByGrade.TryGetValue(gradeId.Value, out var amt))
                {
                    charge = amt;
                }

                return new StudentPlan
                {
                    StudentId = s.Id,
                    GradeId = gradeId,
                    Charge = charge
                };
            }).ToList();

            // Group into preview rows
            var preview = plans
                .GroupBy(p => p.GradeId)
                .Select(g =>
                {
                    var firstCharge = g.First().Charge;
                    return new PreviewRow
                    {
                        GradeId = g.Key ?? Guid.Empty,
                        GradeName = g.Key.HasValue
                            && gradeNames.TryGetValue(g.Key.Value, out var gn)
                                ? gn
                                : "(No grade assigned)",
                        StudentCount = g.Count(),
                        ChargePerStudent = firstCharge,
                        TotalCharge = firstCharge * g.Count()
                    };
                })
                .OrderByDescending(p => p.TotalCharge)
                .ThenBy(p => p.GradeName)
                .ToList();

            return new PlanResult
            {
                ExistingLedgerCount = existingStudentIds.Count,
                NewStudents = plans,
                Preview = preview
            };
        }

        // ---- Internal DTOs ----
        private class PlanResult
        {
            public int ExistingLedgerCount { get; set; }
            public List<StudentPlan> NewStudents { get; set; } = new();
            public List<PreviewRow> Preview { get; set; } = new();
        }

        private class StudentPlan
        {
            public Guid StudentId { get; set; }
            public Guid? GradeId { get; set; }
            public decimal Charge { get; set; }
        }
    }
}