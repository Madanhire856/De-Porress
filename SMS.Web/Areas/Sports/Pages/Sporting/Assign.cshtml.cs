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

namespace SMS.Web.Areas.Sports.Pages.Sporting
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

        public SMS.Data.Sport Sport { get; set; } = null!;
        public List<TeacherRow> Teachers { get; set; } = new();

        /// <summary>Role counts for the header summary — keyed by RoleId.</summary>
        public Dictionary<int, int> RoleCounts { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedStaffIds { get; set; } = new();

        [BindProperty]
        public List<int> SelectedRoleIds { get; set; } = new();

        // =============================================================
        //  ROW MODEL
        //  RoleId is int? — null means "not yet chosen". This is what
        //  gives us the empty "— Select role —" placeholder in the view.
        // =============================================================
        public class TeacherRow
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public bool IsAssigned { get; set; }
            public int? RoleId { get; set; }
        }

        // =============================================================
        //  GET
        // =============================================================
        public async Task<IActionResult> OnGetAsync(Guid sportId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            Sport = await _context.Sports
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sportId);

            if (Sport == null)
                return NotFound();

            await LoadTeachersAsync(sportId);
            return Page();
        }

        // =============================================================
        //  POST
        // =============================================================
        public async Task<IActionResult> OnPostAsync(Guid sportId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            Sport = await _context.Sports
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sportId);

            if (Sport == null)
                return NotFound();

            // Wipe existing assignments for this sport
            var existing = await _context.SportTeachers
                .Where(st => st.SportId == sportId)
                .ToListAsync();

            _context.SportTeachers.RemoveRange(existing);

            // Insert new pairs — SelectedStaffIds[i] ↔ SelectedRoleIds[i]
            var count = Math.Min(SelectedStaffIds.Count, SelectedRoleIds.Count);
            for (int i = 0; i < count; i++)
            {
                if (SelectedStaffIds[i] == Guid.Empty) continue;

                // RoleId 0 is a valid role (HEAD_COACH), so only skip truly
                // invalid entries. The client-side already blocks empty picks,
                // this is a belt-and-braces server guard.
                if (SelectedRoleIds[i] < 0) continue;

                _context.SportTeachers.Add(new SportTeacher
                {
                    Id = Guid.NewGuid(),
                    SportId = sportId,
                    StaffId = SelectedStaffIds[i],
                    RoleId = SelectedRoleIds[i],
                    CreationDate = DateTime.Now,
                    CreatorId = _currentUser.UserId
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // =============================================================
        //  LOAD TEACHERS
        // =============================================================
        private async Task LoadTeachersAsync(Guid sportId)
        {
            // ---- Current assignments (StaffId → RoleId) ----
            var current = await _context.SportTeachers
                .AsNoTracking()
                .Where(st => st.SportId == sportId)
                .ToDictionaryAsync(st => st.StaffId, st => st.RoleId);

            // ---- All active staff ----
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

            // ---- Project to TeacherRow — statement body, not ternary ----
            Teachers = allStaff
                .Where(s => ((StaffCategory)s.CategoryId).CanBeHouseMaster())
                .Select(s =>
                {
                    int? roleId = null;
                    if (current.TryGetValue(s.Id, out var r))
                        roleId = r;

                    return new TeacherRow
                    {
                        StaffId = s.Id,
                        FullName = s.Name + " " + s.Surname,
                        CategoryName = ((StaffCategory)s.CategoryId).ToDisplayName(),
                        IsAssigned = roleId.HasValue,
                        RoleId = roleId
                    };
                })
                .OrderByDescending(t => t.IsAssigned)
                .ThenBy(t => t.FullName)
                .ToList();

            // ---- Role counts — imperative, zero inference risk ----
            RoleCounts = new Dictionary<int, int>();
            foreach (var t in Teachers)
            {
                if (!t.IsAssigned) continue;
                if (!t.RoleId.HasValue) continue;

                var rid = t.RoleId.Value;
                if (RoleCounts.ContainsKey(rid))
                    RoleCounts[rid]++;
                else
                    RoleCounts[rid] = 1;
            }
        }
    }
}