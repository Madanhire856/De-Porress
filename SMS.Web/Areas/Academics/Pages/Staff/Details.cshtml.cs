using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Academics.Pages.Staff
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Data.Staff StaffVM { get; set; } = null!;
        public string? LinkedUserEmail { get; set; }

        public List<string> SubjectsTaught { get; set; } = new();
        public List<string> ClassesLed { get; set; } = new();

        /// <summary>
        /// True when this staff member's category allows teaching features.
        /// </summary>
        public bool IsTeachingStaff { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            StaffVM = _context.Staff
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (StaffVM == null)
                return NotFound();

            // Resolve linked user email
            if (StaffVM.UserId.HasValue)
            {
                LinkedUserEmail = StaffVM.User?.Email
                    ?? _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == StaffVM.UserId.Value)
                        .Select(u => u.Email)
                        .FirstOrDefault();
            }

            IsTeachingStaff = ((StaffCategory)StaffVM.CategoryId).CanTeach();

           
            if (IsTeachingStaff)
            {
                SubjectsTaught = _context.TeacherSubjects
                    .AsNoTracking()
                    .Where(ts => ts.StaffId == StaffVM.Id)
                    .OrderBy(ts => ts.Subject.Name)
                    .Select(ts => ts.Subject.Name)
                    .ToList();

                ClassesLed = _context.Classes
                    .AsNoTracking()
                    .Where(c => c.ClassTeacherId == StaffVM.Id)
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToList();
            }

            return Page();
        }
    }
}