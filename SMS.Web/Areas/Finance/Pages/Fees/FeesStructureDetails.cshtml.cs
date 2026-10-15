using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class FeesStructureDetailsModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public FeesStructureDetailsModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // ---- Display properties ----
        public Guid Id { get; set; }
        public string FeeNumber { get; set; } = "";
        public string GradeName { get; set; } = "—";
        public string TermDisplay { get; set; } = "—";
        public string LevyName { get; set; } = "—";
        public decimal Amount { get; set; }

        public string CurrencyCode { get; set; } = "—";
        public string CurrencyName { get; set; } = "—";
        public string CurrencySymbol { get; set; } = "—";

        public DateTime? CreationDate { get; set; }
        public string? CreatorEmail { get; set; }

        // =============================================================
        //  GET
        // =============================================================
        public IActionResult OnGet(Guid? id)
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            if (id == null || id == Guid.Empty)
                return RedirectToPage("./FeesStructures");

            var entity = _context.FeesStructures
                .AsNoTracking()
                .Include(f => f.Currency)
                .Include(f => f.Creator)
                .FirstOrDefault(f => f.Id == id);

            if (entity == null)
                return NotFound();

            // Populate the [NotMapped] sequence + number
            _context.ApplyFeesStructureNumber(entity);

            Id = entity.Id;
            FeeNumber = entity.FeesStructureNumber;
            Amount = entity.Amount;
            CreationDate = entity.CreationDate;
            CreatorEmail = entity.Creator?.Email;

            // Grade name
            if (entity.GradeId != Guid.Empty)
            {
                GradeName = _context.Grades.AsNoTracking()
                    .Where(g => g.Id == entity.GradeId)
                    .Select(g => g.Name)
                    .FirstOrDefault() ?? "—";
            }

            // Term display
            var term = _context.Terms.AsNoTracking()
                .Where(t => t.Id == entity.TermId)
                .Select(t => new { t.Name, t.AcademicYear, t.Number })
                .FirstOrDefault();

            if (term != null)
            {
                TermDisplay = string.IsNullOrWhiteSpace(term.Name)
                    ? $"{term.AcademicYear} · Term {term.Number}"
                    : term.Name;
            }

            // Levy type
            LevyName = ((LevyType)entity.LevyTypeId).ToDisplayName();

            // Currency
            if (entity.Currency != null)
            {
                CurrencyCode = entity.Currency.Code;
                CurrencyName = entity.Currency.Name;
                CurrencySymbol = entity.Currency.Symbol;
            }

            return Page();
        }
    }
}