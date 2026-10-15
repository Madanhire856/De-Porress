using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Subjects
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Subject SubjectVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public int TeacherCount { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            SubjectVM = _context.Subjects
                .Include(s => s.Creator)
                .FirstOrDefault(s => s.Id == id);

            if (SubjectVM == null)
                return NotFound();

            // Apply generated display number (SUB-0001 etc.)
            _context.ApplySubjectNumber(SubjectVM);

            CreatorEmail = SubjectVM.Creator?.Email;
            TeacherCount = _context.TeacherSubjects
                .Count(ts => ts.SubjectId == SubjectVM.Id);

            return Page();
        }
    }
}