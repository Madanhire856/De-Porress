using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public class BalancesModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public BalancesModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty(SupportsGet = true)] public Guid? TermId { get; set; }
        [BindProperty(SupportsGet = true)] public int? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? GradeId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }

        public List<TermOption> Terms { get; set; } = new();
        public List<GradeOption> Grades { get; set; } = new();
        public List<LedgerRow> Ledgers { get; set; } = new();
        public TermOption? SelectedTerm { get; set; }
        public string BaseCurrency { get; set; } = "USD";

        public class TermOption
        {
            public Guid Id { get; set; }
            public string Display { get; set; } = "";
        }

        public class GradeOption
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class LedgerRow
        {
            public Guid LedgerId { get; set; }
            public Guid StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string GradeName { get; set; } = "—";
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal TotalCharged { get; set; }
            public int StatusId { get; set; }
            public string StatusLabel { get; set; } = "—";
            public string StatusClass { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            BaseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            var today = DateTime.Today;
            var termRows = await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            Terms = termRows.Select(t => new TermOption
            {
                Id = t.Id,
                Display = string.IsNullOrWhiteSpace(t.Name)
                    ? $"{t.AcademicYear} · Term {t.Number}"
                    : t.Name
            }).ToList();

            if (!TermId.HasValue)
            {
                var active = termRows.FirstOrDefault(t => t.StartDate <= today && t.EndDate >= today);
                TermId = active?.Id ?? termRows.FirstOrDefault()?.Id;
            }

            SelectedTerm = Terms.FirstOrDefault(t => t.Id == TermId);

            Grades = await _context.Grades.AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new GradeOption { Id = g.Id, Name = g.Name })
                .ToListAsync();

            if (!TermId.HasValue) return;

            var query = _context.StudentLedgers
                .AsNoTracking()
                .Include(l => l.Student)
                .Where(l => l.TermId == TermId.Value);

            if (StatusFilter.HasValue)
                query = query.Where(l => (l.StatusId ?? 0) == StatusFilter.Value);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim();
                query = query.Where(l => l.Student.Name.Contains(s) || l.Student.Surname.Contains(s));
            }

            var ledgers = await query.ToListAsync();

            var ledgerIds = ledgers.Select(l => l.Id).ToList();
            var studentIds = ledgers.Select(l => l.StudentId).Distinct().ToList();

            // Class → Grade lookups
            var studentClassMap = await _context.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, s.ClassId })
                .ToDictionaryAsync(x => x.Id, x => x.ClassId);

            var classToGrade = await _context.Classes.AsNoTracking()
                .Select(c => new { c.Id, c.GradeId })
                .ToDictionaryAsync(x => x.Id, x => x.GradeId);

            var gradeNames = await _context.Grades.AsNoTracking()
                .Select(g => new { g.Id, g.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            // Paid per ledger (base-currency sum)
            var paidByLedger = await _context.Payments.AsNoTracking()
                .Where(p => p.LedgerId.HasValue && ledgerIds.Contains(p.LedgerId.Value))
                .GroupBy(p => p.LedgerId!.Value)
                .Select(g => new
                {
                    LedgerId = g.Key,
                    Total = g.Sum(p => p.BaseAmount ?? p.Amount ?? 0m)
                })
                .ToDictionaryAsync(x => x.LedgerId, x => x.Total);

            // Grade filter applied after lookups
            IEnumerable<StudentLedger> filtered = ledgers;
            if (GradeId.HasValue)
            {
                filtered = ledgers.Where(l =>
                {
                    if (!studentClassMap.TryGetValue(l.StudentId, out var cid) || !cid.HasValue)
                        return false;
                    return classToGrade.TryGetValue(cid.Value, out var gid) && gid == GradeId.Value;
                });
            }

            Ledgers = filtered.Select(l =>
            {
                var paid = paidByLedger.TryGetValue(l.Id, out var p) ? p : 0m;
                var statusId = l.StatusId ?? 0;

                var (label, css) = statusId switch
                {
                    (int)StudentLedgerStatus.SETTLED => ("Settled", "status-active"),
                    (int)StudentLedgerStatus.PARTIALLY_SETTLED => ("Partial", "status-count"),
                    (int)StudentLedgerStatus.OVER_DUE => ("Overdue", "status-overdue"),
                    _ => ("—", "status-count")
                };

                string gradeName = "—";
                if (studentClassMap.TryGetValue(l.StudentId, out var cid2) && cid2.HasValue
                    && classToGrade.TryGetValue(cid2.Value, out var gid2)
                    && gradeNames.TryGetValue(gid2, out var gn))
                {
                    gradeName = gn;
                }

                return new LedgerRow
                {
                    LedgerId = l.Id,
                    StudentId = l.StudentId,
                    StudentName = $"{l.Student.Name} {l.Student.Surname}".Trim(),
                    GradeName = gradeName,
                    OpeningBalance = l.OpeningBalance,
                    ClosingBalance = l.ClosingBalance,
                    TotalPaid = paid,
                    TotalCharged = l.OpeningBalance,
                    StatusId = statusId,
                    StatusLabel = label,
                    StatusClass = css
                };
            })
            .OrderBy(r => r.StudentName)
            .ToList();
        }
    }
}