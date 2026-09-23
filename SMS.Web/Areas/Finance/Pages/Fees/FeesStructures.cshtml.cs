using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class FeesStructuresModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public FeesStructuresModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public List<RowVM> Rows { get; set; } = new();

        public class RowVM
        {
            public Guid Id { get; set; }
            public string FeeNumber { get; set; } = "";
            public string GradeName { get; set; } = "";
            public string TermDisplay { get; set; } = "";
            public string LevyName { get; set; } = "";
            public decimal Amount { get; set; }
            public string CurrencyCode { get; set; } = "";
        }

        // =============================================================
        //  GET — list only
        // =============================================================
        public async Task<IActionResult> OnGetAsync()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            await LoadRowsAsync();
            return Page();
        }

        // =============================================================
        //  POST — delete
        // =============================================================
        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            var row = await _context.FeesStructures
                .FirstOrDefaultAsync(f => f.Id == id);

            if (row != null)
            {
                _context.FeesStructures.Remove(row);
                await _context.SaveChangesAsync();
                TempData["FeesSuccess"] = "Fee removed.";
            }

            return RedirectToPage("./FeesStructures");
        }

        // =============================================================
        //  LOAD
        // =============================================================
        private async Task LoadRowsAsync()
        {
            var rows = await _context.FeesStructures
                .AsNoTracking()
                .Include(f => f.Currency)
                .ToListAsync();

            // Populate the [NotMapped] sequence on each row
            _context.ApplyFeesStructureNumbers(rows);

            var gradeNames = await _context.Grades.AsNoTracking()
                .Select(g => new { g.Id, g.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var termDisplay = (await _context.Terms.AsNoTracking()
                .Select(t => new { t.Id, t.Name, t.AcademicYear, t.Number })
                .ToListAsync())
                .ToDictionary(
                    t => t.Id,
                    t => string.IsNullOrWhiteSpace(t.Name)
                        ? $"{t.AcademicYear} · Term {t.Number}"
                        : t.Name);

            Rows = rows.Select(f => new RowVM
            {
                Id = f.Id,
                FeeNumber = f.FeesStructureNumber,
                GradeName = gradeNames.TryGetValue(f.GradeId, out var gn) ? gn : "—",
                TermDisplay = termDisplay.TryGetValue(f.TermId, out var tn) ? tn : "—",
                LevyName = ((LevyType)f.LevyTypeId).ToDisplayName(),
                Amount = f.Amount,
                CurrencyCode = f.Currency?.Code ?? "—"
            })
            .OrderBy(r => r.GradeName)
            .ThenBy(r => r.TermDisplay)
            .ThenBy(r => r.LevyName)
            .ToList();
        }
    }
}