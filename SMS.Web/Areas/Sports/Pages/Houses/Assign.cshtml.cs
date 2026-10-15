using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Sports.Pages.Houses
{
    [Authorize]
    public class AssignModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AssignModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public House House { get; set; } = null!;
        public List<TeacherRow> Teachers { get; set; } = new();

        // Role counts for the header summary (keyed by RoleId)
        public Dictionary<int, int> RoleCounts { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedStaffIds { get; set; } = new();

        [BindProperty]
        public List<int> SelectedRoleIds { get; set; } = new();

        public class TeacherRow
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public bool IsAssigned { get; set; }
            public int RoleId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid houseId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            House = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId);

            if (House == null)
                return NotFound();

            await LoadTeachersAsync(houseId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid houseId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            House = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId);

            if (House == null)
                return NotFound();

            // Delete existing rows for this house
            var existing = await _context.HouseTeachers
                .Where(ht => ht.HouseId == houseId)
                .ToListAsync();

            _context.HouseTeachers.RemoveRange(existing);

            // Insert submitted pairs (SelectedStaffIds[i] ↔ SelectedRoleIds[i])
            var count = Math.Min(SelectedStaffIds.Count, SelectedRoleIds.Count);
            for (int i = 0; i < count; i++)
            {
                if (SelectedStaffIds[i] == Guid.Empty) continue;

                _context.HouseTeachers.Add(new HouseTeacher
                {
                    Id = Guid.NewGuid(),
                    HouseId = houseId,
                    StaffId = SelectedStaffIds[i],
                    RoleId = SelectedRoleIds[i],
                    CreationDate = DateTime.Now,
                    CreatorId = _currentUser.UserId
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadTeachersAsync(Guid houseId)
        {
            // Current assignments for this house (StaffId -> RoleId)
            var current = await _context.HouseTeachers
                .AsNoTracking()
                .Where(ht => ht.HouseId == houseId)
                .ToDictionaryAsync(ht => ht.StaffId, ht => ht.RoleId);

            // All active staff
            var allStaff = await _context.Staff
                .AsNoTracking()
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Surname,
                    s.CategoryId
                })
                .ToListAsync();

            Teachers = allStaff
                .Where(s => ((StaffCategory)s.CategoryId).CanBeHouseMaster())
                .Select(s => new TeacherRow
                {
                    StaffId = s.Id,
                    FullName = s.Name + " " + s.Surname,
                    CategoryName = ((StaffCategory)s.CategoryId).ToDisplayName(),
                    IsAssigned = current.ContainsKey(s.Id),
                    RoleId = current.TryGetValue(s.Id, out var r)
                        ? r
                        : (int)TeacherHouseRole.ASSISTANT
                })
                .OrderByDescending(t => t.IsAssigned)  
                .ThenBy(t => t.FullName)
                .ToList();

            // Role counts for the header summary
            RoleCounts = Teachers
                .Where(t => t.IsAssigned)
                .GroupBy(t => t.RoleId)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}