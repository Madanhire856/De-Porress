using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Areas.Config.Pages.Terms.ViewModels;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Terms
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public TermVM TermVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            var entity = _context.Terms
                .Include(t => t.Creator)
                .FirstOrDefault(t => t.Id == id);

            if (entity == null)
                return NotFound();

            _context.ApplyTermNumber(entity);

            TermVM = new TermVM
            {
                Id = entity.Id,
                Name = entity.Name ?? "",
                AcademicYear = entity.AcademicYear,
                TermNumber = entity.Number,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                CreationDate = entity.CreationDate,
                TermDisplayId = (entity as dynamic).TermDisplayId ?? ""
            };

            CreatorEmail = entity.Creator?.Email;

            return Page();
        }
    }
}