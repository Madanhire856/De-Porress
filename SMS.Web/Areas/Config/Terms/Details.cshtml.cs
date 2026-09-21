using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
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

        public Term TermVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            TermVM = _context.Terms
                .Include(t => t.Creator)
                .FirstOrDefault(t => t.Id == id);

            if (TermVM == null)
                return NotFound();

            _context.ApplyTermNumber(TermVM);
            CreatorEmail = TermVM.Creator?.Email;

            return Page();
        }
    }
}