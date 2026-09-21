using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Classes
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Data.Class ClassVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public string? TeacherName { get; set; }
        public string? GradeName { get; set; }
        public int StudentCount { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            ClassVM = _context.Classes
                .Include(c => c.Creator)
                .Include(c => c.ClassTeacher)
                .Include(c => c.Grade)
                .FirstOrDefault(c => c.Id == id);

            if (ClassVM == null)
                return NotFound();

            _context.ApplyClassNumber(ClassVM);

            CreatorEmail = ClassVM.Creator?.Email;

            TeacherName = ClassVM.ClassTeacher != null
                ? $"{ClassVM.ClassTeacher.Name} {ClassVM.ClassTeacher.Surname}"
                : null;

            GradeName = ClassVM.Grade?.Name;
            StudentCount = _context.Students
                .Count(s => s.ClassId.HasValue && s.ClassId.Value == ClassVM.Id);

            return Page();
        }
    }
}