using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Staff.ViewModels; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Academics.Pages.Staff  
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
        public StaffVM staff { get; set; } = new();

        public List<SelectListItem> Users { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> MaritalStatuses { get; set; } = new();
        public List<SelectListItem> Genders { get; set; } = new();
        public List<SelectListItem> Qualifications { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateStaff))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditStaff))
                return Forbid();

            await LoadLookupsAsync();

            if (id != null)
            {
                // === EDIT ===
                var entity = await _context.Staff
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity != null)
                {
                    staff = new StaffVM
                    {
                        Id = entity.Id,
                        UserId = entity.UserId,
                        EcNumber = entity.EcNumber,
                        Name = entity.Name,
                        Surname = entity.Surname,
                        CategoryId = entity.CategoryId,
                        Age = entity.Age,
                        MaritalStatusId = entity.MaritalStatusId,
                        GenderId = entity.GenderId,
                        QualificationId = entity.QualificationId,
                        DateJoined = entity.DateJoined,
                        IdNumber = entity.IdNumber,
                        IsActive = entity.IsActive ?? false
                    };
                }
            }
            else
            {
                // === CREATE ===
                staff = new StaffVM
                {
                    Id = Guid.NewGuid(),
                    IsActive = true
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateStaff))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditStaff))
                return Forbid();

            await LoadLookupsAsync();

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            // --- If a user is linked, make sure it exists and isn't already taken ---
            if (staff.UserId.HasValue && staff.UserId.Value != Guid.Empty)
            {
                bool userExists = await _context.Users
                    .AnyAsync(u => u.Id == staff.UserId.Value);

                if (!userExists)
                {
                    ModelState.AddModelError("staff.UserId", "The selected user account no longer exists.");
                    return Page();
                }

                var clashUser = await _context.Staff
                    .FirstOrDefaultAsync(s => s.UserId == staff.UserId.Value && s.Id != excludeId);

                if (clashUser != null)
                {
                    ModelState.AddModelError("staff.UserId", "This user is already linked to another staff member.");
                    return Page();
                }
            }
            else
            {
                staff.UserId = null;
            }

            // Uniqueness — EC Number
            var clashEc = await _context.Staff
                .FirstOrDefaultAsync(s => s.EcNumber == staff.EcNumber && s.Id != excludeId);
            if (clashEc != null)
            {
                ModelState.AddModelError("staff.EcNumber", "This EC Number is already in use.");
                return Page();
            }

            // Uniqueness — ID Number
            var clashId = await _context.Staff
                .FirstOrDefaultAsync(s => s.IdNumber == staff.IdNumber && s.Id != excludeId);
            if (clashId != null)
            {
                ModelState.AddModelError("staff.IdNumber", "This ID Number is already in use.");
                return Page();
            }

            Data.Staff entity;

            if (id == null)
            {
                // === CREATE ===
                entity = new Data.Staff
                {
                    Id = staff.Id == Guid.Empty ? Guid.NewGuid() : staff.Id
                };
                await _context.Staff.AddAsync(entity);
            }
            else
            {
                // === EDIT ===
                entity = await _context.Staff
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            // Map form → entity (both insert and update)
            entity.UserId = staff.UserId;
            entity.EcNumber = staff.EcNumber;
            entity.Name = staff.Name;
            entity.Surname = staff.Surname;
            entity.CategoryId = staff.CategoryId ?? 0;
            entity.Age = staff.Age;
            entity.MaritalStatusId = staff.MaritalStatusId ?? 0;
            entity.GenderId = staff.GenderId ?? 0;
            entity.QualificationId = staff.QualificationId ?? 0;
            entity.DateJoined = staff.DateJoined ?? DateTime.Today;
            entity.IdNumber = staff.IdNumber;
            entity.IsActive = staff.IsActive;

            await _context.SaveChangesAsync();

            // 👇 area changed from Config to Academics
            return RedirectToPage("./Details", new { area = "Academics", id = entity.Id });
        }

        private async Task LoadLookupsAsync()
        {
            // Users — exclude those already linked to another staff member
            var linkedUserIds = await _context.Staff
                .Where(s => s.UserId != null)
                .Select(s => s.UserId!.Value)
                .ToListAsync();

            Users = await _context.Users
                .AsNoTracking()
                .Where(u => !linkedUserIds.Contains(u.Id))
                .OrderBy(u => u.Email)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name != null ? u.Name + " (" + u.Email + ")" : u.Email
                })
                .ToListAsync();

            Genders = EnumExtensions.AllGenders()
                .Select(g => new SelectListItem
                {
                    Value = ((int)g).ToString(),
                    Text = g.ToDisplayName()
                })
                .ToList();

            MaritalStatuses = EnumExtensions.AllMaritalStatuses()
                .Select(m => new SelectListItem
                {
                    Value = ((int)m).ToString(),
                    Text = m.ToDisplayName()
                })
                .ToList();

            Categories = EnumExtensions.AllStaffCategories()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToDisplayName()
                })
                .ToList();

            Qualifications = EnumExtensions.AllQualifications()
                .Select(q => new SelectListItem
                {
                    Value = ((int)q).ToString(),
                    Text = q.ToDisplayName()
                })
                .ToList();
        }
    }
}