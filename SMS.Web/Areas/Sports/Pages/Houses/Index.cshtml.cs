using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Sports.Pages.Houses
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<HouseRow> Houses { get; set; } = new();

        // Header stats
        public int TotalHouses => Houses.Count;
        public int TotalAssigned => Houses.Sum(h => h.TeacherCount);
        public int HousesWithoutMaster => Houses.Count(h => h.Master == null);

        public class HouseRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string Color { get; set; } = "";

            public List<TeacherItem> Teachers { get; set; } = new();

            public TeacherItem? Master =>
                Teachers.FirstOrDefault(t => t.RoleId == (int)TeacherHouseRole.MASTER);

            public IEnumerable<TeacherItem> Assistants =>
                Teachers.Where(t => t.RoleId != (int)TeacherHouseRole.MASTER);

            public int TeacherCount => Teachers.Count;
        }

        public class TeacherItem
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public int RoleId { get; set; }
            public string RoleName { get; set; } = "";
            public string CategoryName { get; set; } = "";
        }

        public void OnGet()
        {
            var houses = _context.Houses
                .AsNoTracking()
                .OrderBy(h => h.Name)
                .ToList();

            var links = _context.HouseTeachers
                .AsNoTracking()
                .Include(ht => ht.Staff)
                .Select(ht => new
                {
                    ht.HouseId,
                    ht.StaffId,
                    ht.RoleId,
                    Name = ht.Staff.Name,
                    Surname = ht.Staff.Surname,
                    CategoryId = ht.Staff.CategoryId
                })
                .ToList();

            var grouped = links
                .GroupBy(l => l.HouseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Houses = houses.Select(h =>
            {
                var assigned = grouped.TryGetValue(h.Id, out var list) ? list : new();

                var teachers = assigned
                    .Select(a => new TeacherItem
                    {
                        StaffId = a.StaffId,
                        FullName = $"{a.Name} {a.Surname}".Trim(),
                        RoleId = a.RoleId,
                        RoleName = ((TeacherHouseRole)a.RoleId).ToDisplayName(),
                        CategoryName = ((StaffCategory)a.CategoryId).ToDisplayName()
                    })
                    // Master first, then alphabetical
                    .OrderBy(t => t.RoleId)
                    .ThenBy(t => t.FullName)
                    .ToList();

                return new HouseRow
                {
                    Id = h.Id,
                    Name = h.Name,
                    Color = h.Color,
                    Teachers = teachers
                };
            }).ToList();
        }
    }
}