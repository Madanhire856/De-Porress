using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Prefects.ViewModels;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Academics.Pages.Prefects
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DetailsModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public PrefectVM Prefect { get; set; } = null!;
        public string? CreatorEmail { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (!_currentUser.HasRight(AccessRights.ViewPrefects))
                return Forbid();

            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            var entity = _context.Prefects
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s.Class)
                .Include(p => p.Nominator)
                .Include(p => p.Creator)
                .FirstOrDefault(p => p.Id == id.Value);

            if (entity == null)
                return NotFound();

            // Populate student number
            var studentList = new List<Student> { entity.Student };
            _context.ApplyStudentNumbers(studentList);

            Prefect = new PrefectVM
            {
                Id = entity.Id,
                StudentId = entity.Student.Id,
                StudentName = entity.Student.Name,
                StudentSurname = entity.Student.Surname,
                StudentNumber = entity.Student.StudentNumber,
                StudentClass = entity.Student.Class?.Name ?? "—",
                StudentPhotoUrl = entity.Student.PhotoUrl,
                StudentGenderId = entity.Student.GenderId,
                StudentDob = entity.Student.Dob,
                PostTitleId = entity.PostTitleId,
                StatusId = entity.StatusId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                NominatorId = entity.NominatorId,
                NominatorName = entity.Nominator != null
                    ? $"{entity.Nominator.Name} {entity.Nominator.Surname}".Trim()
                    : null,

                // ---- Metadata ----
                CreationDate = entity.CreationDate
            };

            CreatorEmail = entity.Creator?.Email;

            return Page();
        }
    }
}