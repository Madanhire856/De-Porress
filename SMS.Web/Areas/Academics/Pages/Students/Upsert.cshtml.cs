using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Students.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Academics.Pages.Students
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpsertModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty]
        public StudentVM Student { get; set; } = new();

        public bool IsEdit { get; set; }

        // All dropdown option lists live on the PageModel
        public List<SelectListItem> GenderOptions { get; set; } = new();
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> EnrolmentStatusOptions { get; set; } = new();
        public List<SelectListItem> BaptismStatusOptions { get; set; } = new();
        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> VillageOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null && !_currentUser.HasRight(AccessRights.CreateLearners))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditLearners))
                return Forbid();

            if (id != null)
            {
                var entity = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (entity == null) return NotFound();

                IsEdit = true;
                Student = new StudentVM
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Surname = entity.Surname,
                    GenderId = entity.GenderId,
                    Dob = entity.Dob,
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

                    CreationDate = entity.CreationDate
                };
            }
            else
            {
                Student = new StudentVM
                {
                    Id = Guid.NewGuid(),
                    EnrolmentDate = DateTime.Today
                };
            }

            await LoadOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null && !_currentUser.HasRight(AccessRights.CreateLearners))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditLearners))
                return Forbid();

            if (!Student.IsCatholic)
                Student.BaptismStatusId = null;

            if (Student.Dob.HasValue && Student.Dob.Value > DateTime.Today)
            {
                ModelState.AddModelError("Student.Dob", "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                IsEdit = id != null;
                return Page();
            }

            Student entity;

            if (id == null)
            {
                entity = new Student
                {
                    Id = Student.Id == Guid.Empty ? Guid.NewGuid() : Student.Id,
                    CreatorId = _currentUser.UserId,
                    CreationDate = DateTime.Now
                };
                await _context.Students.AddAsync(entity);
            }
            else
            {
                entity = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
                if (entity == null) return NotFound();
            }

            entity.Name = Student.Name.Trim();
            entity.Surname = Student.Surname.Trim();
            entity.GenderId = Student.GenderId;
            entity.Dob = Student.Dob;
            entity.BirthEntryNumber = Trim(Student.BirthEntryNumber);
            entity.PhotoUrl = Trim(Student.PhotoUrl);

            entity.CategoryId = Student.CategoryId;
            entity.EnrolmentStatusId = Student.EnrolmentStatusId;
            entity.ClassId = Student.ClassId;
            entity.EnrolmentDate = Student.EnrolmentDate;

            entity.IsCatholic = Student.IsCatholic;
            entity.BaptismStatusId = Student.IsCatholic ? Student.BaptismStatusId : null;

            entity.VillageId = Student.VillageId;

            entity.AllergieNotesJson = Trim(Student.AllergieNotesJson);
            entity.DisabilityNotesJson = Trim(Student.DisabilityNotesJson);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = entity.Id });
        }

        private static string? Trim(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private async Task LoadOptionsAsync()
        {
            // Gender
            GenderOptions = EnumExtensions.AllGenders()
                .Select(g => new SelectListItem
                {
                    Value = ((int)g).ToString(),
                    Text = g.ToDisplayName()
                })
                .ToList();
            GenderOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select gender --" });

            // Category
            CategoryOptions = EnumExtensions.AllStudentCategories()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToDisplayName()
                })
                .ToList();
            CategoryOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select category --" });

            // Enrolment Status
            EnrolmentStatusOptions = EnumExtensions.AllEnrolmentStatuses()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToDisplayName()
                })
                .ToList();
            EnrolmentStatusOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select enrolment status --" });

            // Baptism Status
            BaptismStatusOptions = EnumExtensions.AllBaptismStatuses()
                .Select(b => new SelectListItem
                {
                    Value = ((int)b).ToString(),
                    Text = b.ToDisplayName()
                })
                .ToList();
            BaptismStatusOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select baptism status --" });

            // Class
            ClassOptions = await _context.Classes
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
            ClassOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select class --" });

            // Village
            VillageOptions = await _context.Villages
                .AsNoTracking()
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Name
                })
                .ToListAsync();
            VillageOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Select village --" });
        }
    }
}