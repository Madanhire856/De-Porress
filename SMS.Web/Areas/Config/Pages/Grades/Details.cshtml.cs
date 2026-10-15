using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Grades
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Grade GradeVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public int ClassCount { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            GradeVM = _context.Grades
                .Include(g => g.Creator)
                .FirstOrDefault(g => g.Id == id);

            if (GradeVM == null)
                return NotFound();
            _context.ApplyGradeNumber(GradeVM);

            CreatorEmail = GradeVM.Creator?.Email;
            ClassCount = _context.Classes.Count(c => c.GradeId == GradeVM.Id);

            return Page();
        }
    }
}