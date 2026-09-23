using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Students.ViewModels;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Academics.Pages.Students
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

        public StudentVM Student { get; set; } = null!;
        public string? CreatorEmail { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (!_currentUser.HasRight(AccessRights.ViewLearners))
                return Forbid();

            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            var entity = _context.Students
                .AsNoTracking()
                .Include(s => s.Class)
                .Include(s => s.Village)
                .Include(s => s.Creator)
                .FirstOrDefault(s => s.Id == id);

            if (entity == null)
                return NotFound();

            // Populate the generated student number
            _context.ApplyStudentNumber(entity);

            Student = new StudentVM
            {
                Id = entity.Id,
                StudentNumber = entity.StudentNumber,
                Name = entity.Name,
                Surname = entity.Surname,
                Dob = entity.Dob,
                GenderId = entity.GenderId,
                BirthEntryNumber = entity.BirthEntryNumber,
                PhotoUrl = entity.PhotoUrl,

                CategoryId = entity.CategoryId,
                EnrolmentStatusId = entity.EnrolmentStatusId,
                ClassId = entity.ClassId,
                EnrolmentDate = entity.EnrolmentDate,

                IsCatholic = entity.IsCatholic ?? false,
                BaptismStatusId = entity.BaptismStatusId,

                VillageId = entity.VillageId,

                AllergieNotesJson = entity.AllergieNotesJson,
                DisabilityNotesJson = entity.DisabilityNotesJson,

                // Related-entity display fields
                ClassName = entity.Class?.Name ?? "—",
                VillageName = entity.Village?.Name ?? "—",
                VillageHeadman = entity.Village?.Headman,
                VillageChief = entity.Village?.Chief,

                // Metadata
                CreationDate = entity.CreationDate
            };

            CreatorEmail = entity.Creator?.Email;

            return Page();
        }
    }
}