using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public IndexModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // DASHBOARD TILES
        public int ActiveStudentCount { get; set; }
        public int ActiveLedgerCount { get; set; }
        public int OverdueCount { get; set; }
        public int PartiallySettledCount { get; set; }
        public int SettledCount { get; set; }

        // Money — decimal
        public decimal TotalOutstanding { get; set; }
        public decimal TotalCollectedThisTerm { get; set; }
        public decimal TotalChargedThisTerm { get; set; }

        public int FeesStructureCount { get; set; }

        public string CurrentTermName { get; set; } = "No active term";

        // Computed — feeds the collections progress bar
        public int CollectedPercentage =>
            TotalChargedThisTerm > 0m
                ? (int)Math.Round((double)TotalCollectedThisTerm / (double)TotalChargedThisTerm * 100)
                : 0;

        // Recent activity
        public List<RecentPayment> RecentPayments { get; set; } = new();
        public List<TopDebtor> TopDebtors { get; set; } = new();

        // Fee structure preview
        public List<FeeStructurePreview> FeeStructures { get; set; } = new();

        public class RecentPayment
        {
            public Guid Id { get; set; }
            public string StudentName { get; set; } = "";
            public decimal Amount { get; set; }        // ← was int
            public string Method { get; set; } = "";
            public DateTime Date { get; set; }
            public Guid LedgerId { get; set; }
        }

        public class TopDebtor
        {
            public Guid LedgerId { get; set; }
            public Guid StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string GradeName { get; set; } = "—";
            public decimal Balance { get; set; }       // ← was int
        }

        public class FeeStructurePreview
        {
            public Guid GradeId { get; set; }
            public string GradeName { get; set; } = "";
            public decimal TotalAmount { get; set; }   // ← was int
            public string CurrencyCode { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            ActiveStudentCount = _context.Students
                .AsNoTracking()
                .Count(s => s.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE);

            FeesStructureCount = _context.FeesStructures
                .AsNoTracking()
                .Count();

            var today = DateTime.Today;

            var currentTerm = _context.Terms
                .AsNoTracking()
                .Where(t => t.StartDate <= today && t.EndDate >= today)
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();

            if (currentTerm != null)
            {
                CurrentTermName = string.IsNullOrWhiteSpace(currentTerm.Name)
                    ? $"{currentTerm.AcademicYear} · Term {currentTerm.Number}"
                    : currentTerm.Name;
            }

            if (currentTerm != null)
            {
                var ledgers = _context.StudentLedgers
                    .AsNoTracking()
                    .Where(l => l.TermId == currentTerm.Id)
                    .ToList();

                ActiveLedgerCount = ledgers.Count;

                var ledgerIds = ledgers.Select(l => l.Id).ToList();

                // Sum BaseAmount — the base-currency equivalent — as decimal
                var paidByLedger = _context.Payments
                    .AsNoTracking()
                    .Where(p => p.LedgerId.HasValue && ledgerIds.Contains(p.LedgerId.Value))
                    .GroupBy(p => p.LedgerId!.Value)
                    .Select(g => new
                    {
                        LedgerId = g.Key,
                        Total = g.Sum(p => p.BaseAmount ?? p.Amount ?? 0m)
                    })
                    .ToDictionary(x => x.LedgerId, x => x.Total);

                var classToGrade = _context.Classes
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.GradeId })
                    .ToDictionary(x => x.Id, x => x.GradeId);

                var chargeByGradeTerm = _context.FeesStructures
                    .AsNoTracking()
                    .Where(f => f.TermId == currentTerm.Id)
                    .GroupBy(f => f.GradeId)
                    .Select(g => new { GradeId = g.Key, Total = g.Sum(f => f.Amount) })
                    .ToDictionary(x => x.GradeId, x => x.Total);

                var studentIds = ledgers.Select(l => l.StudentId).Distinct().ToList();
                var studentClassMap = _context.Students
                    .AsNoTracking()
                    .Where(s => studentIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.ClassId })
                    .ToDictionary(x => x.Id, x => x.ClassId);

                decimal charged = 0m;
                decimal paid = 0m;

                foreach (var l in ledgers)
                {
                    decimal charge = 0m;
                    if (studentClassMap.TryGetValue(l.StudentId, out var classId) && classId.HasValue)
                    {
                        if (classToGrade.TryGetValue(classId.Value, out var gid) &&
                            chargeByGradeTerm.TryGetValue(gid, out var amt))
                        {
                            charge = amt;
                        }
                    }

                    charged += l.OpeningBalance + charge;
                    paid += paidByLedger.TryGetValue(l.Id, out var p) ? p : 0m;
                }

                TotalChargedThisTerm = charged;
                TotalCollectedThisTerm = paid;
                TotalOutstanding = Math.Max(0m, charged - paid);

                SettledCount = ledgers.Count(l => (l.StatusId ?? 0) == (int)StudentLedgerStatus.SETTLED);
                OverdueCount = ledgers.Count(l => (l.StatusId ?? 0) == (int)StudentLedgerStatus.OVER_DUE);
                PartiallySettledCount = ledgers.Count(l => (l.StatusId ?? 0) == (int)StudentLedgerStatus.PARTIALLY_SETTLED);
            }

            // ---- Recent payments ----
            var recentPaymentRows = _context.Payments
                .AsNoTracking()
                .Where(p => p.LedgerId.HasValue)
                .OrderByDescending(p => p.CreationDate)
                .Take(8)
                .ToList();

            var recentLedgerIds = recentPaymentRows
                .Select(p => p.LedgerId!.Value)
                .Distinct()
                .ToList();

            var recentLedgers = _context.StudentLedgers
                .AsNoTracking()
                .Include(l => l.Student)
                .Where(l => recentLedgerIds.Contains(l.Id))
                .ToDictionary(l => l.Id, l => l);

            RecentPayments = recentPaymentRows
                .Select(p =>
                {
                    var ledger = p.LedgerId.HasValue
                        && recentLedgers.TryGetValue(p.LedgerId.Value, out var l)
                        ? l
                        : null;
                    var method = EnumExtensions.ParsePaymentMethod(p.PaymentMethodId);

                    return new RecentPayment
                    {
                        Id = p.Id,
                        StudentName = ledger != null
                            ? $"{ledger.Student.Name} {ledger.Student.Surname}".Trim()
                            : "—",
                        Amount = p.BaseAmount ?? p.Amount ?? 0m,
                        Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                        Date = p.CreationDate,
                        LedgerId = p.LedgerId ?? Guid.Empty
                    };
                })
                .Where(r => r.LedgerId != Guid.Empty)
                .ToList();

            // ---- Top debtors ----
            if (currentTerm != null)
            {
                var classToGradeForDebtors = _context.Classes
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.GradeId })
                    .ToDictionary(x => x.Id, x => x.GradeId);

                var gradeNames = _context.Grades
                    .AsNoTracking()
                    .Select(g => new { g.Id, g.Name })
                    .ToDictionary(x => x.Id, x => x.Name);

                var chargeByGradeTerm2 = _context.FeesStructures
                    .AsNoTracking()
                    .Where(f => f.TermId == currentTerm.Id)
                    .GroupBy(f => f.GradeId)
                    .Select(g => new { GradeId = g.Key, Total = g.Sum(f => f.Amount) })
                    .ToDictionary(x => x.GradeId, x => x.Total);

                var ledgerList = _context.StudentLedgers
                    .AsNoTracking()
                    .Include(l => l.Student)
                    .Where(l => l.TermId == currentTerm.Id)
                    .ToList();

                var ledgerIds2 = ledgerList.Select(l => l.Id).ToList();

                var paidMap = _context.Payments
                    .AsNoTracking()
                    .Where(p => p.LedgerId.HasValue && ledgerIds2.Contains(p.LedgerId.Value))
                    .GroupBy(p => p.LedgerId!.Value)
                    .Select(g => new
                    {
                        LedgerId = g.Key,
                        Total = g.Sum(p => p.BaseAmount ?? p.Amount ?? 0m)
                    })
                    .ToDictionary(x => x.LedgerId, x => x.Total);

                TopDebtors = ledgerList
                    .Select(l =>
                    {
                        decimal charge = 0m;
                        Guid? gradeId = null;

                        if (l.Student.ClassId.HasValue &&
                            classToGradeForDebtors.TryGetValue(l.Student.ClassId.Value, out var cg))
                        {
                            gradeId = cg;
                            if (chargeByGradeTerm2.TryGetValue(cg, out var amt))
                                charge = amt;
                        }

                        var paid = paidMap.TryGetValue(l.Id, out var p) ? p : 0m;
                        var balance = l.OpeningBalance + charge - paid;

                        return new TopDebtor
                        {
                            LedgerId = l.Id,
                            StudentId = l.StudentId,
                            StudentName = $"{l.Student.Name} {l.Student.Surname}".Trim(),
                            GradeName = gradeId.HasValue && gradeNames.TryGetValue(gradeId.Value, out var gn)
                                ? gn
                                : "—",
                            Balance = balance
                        };
                    })
                    .Where(d => d.Balance > 0m)
                    .OrderByDescending(d => d.Balance)
                    .Take(5)
                    .ToList();
            }

            // ---- Fee structure preview ----
            if (currentTerm != null)
            {
                var structures = _context.FeesStructures
                    .AsNoTracking()
                    .Include(f => f.Currency)
                    .Where(f => f.TermId == currentTerm.Id)
                    .ToList();

                var gradeNames2 = _context.Grades
                    .AsNoTracking()
                    .Select(g => new { g.Id, g.Name })
                    .ToDictionary(x => x.Id, x => x.Name);

                FeeStructures = structures
                    .GroupBy(f => f.GradeId)
                    .Select(g => new FeeStructurePreview
                    {
                        GradeId = g.Key,
                        GradeName = gradeNames2.TryGetValue(g.Key, out var gn) ? gn : "—",
                        TotalAmount = g.Sum(f => f.Amount),
                        CurrencyCode = g.FirstOrDefault()?.Currency?.Code ?? "—"
                    })
                    .OrderBy(f => f.GradeName)
                    .ToList();
            }

            return Page();
        }
    }
}