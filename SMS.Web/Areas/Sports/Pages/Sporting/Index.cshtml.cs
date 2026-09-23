using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Sports.Pages.Sporting
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<SportRow> Sports { get; set; } = new();

        public int TotalSports => Sports.Count;
        public int TotalCoaches => Sports.Sum(s => s.CoachCount);
        public int SportsWithoutHeadCoach => Sports.Count(s => s.HeadCoach == null);
        public int TotalCompetitions => Sports.Sum(s => s.CompetitionCount);

        public class SportRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";

            public List<CoachItem> Coaches { get; set; } = new();
            public int CompetitionCount { get; set; }

            public CoachItem? HeadCoach =>
                Coaches.FirstOrDefault(c => c.RoleId == (int)TeacherSportRole.HEAD_COACH);

            public IEnumerable<CoachItem> Assistants =>
                Coaches.Where(c => c.RoleId != (int)TeacherSportRole.HEAD_COACH);

            public int CoachCount => Coaches.Count;
        }

        public class CoachItem
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public int RoleId { get; set; }
            public string RoleName { get; set; } = "";
            public string CategoryName { get; set; } = "";
        }

        public void OnGet()
        {
            var sports = _context.Sports
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToList();

            var links = _context.SportTeachers
                .AsNoTracking()
                .Include(st => st.Staff)
                .Select(st => new
                {
                    st.SportId,
                    st.StaffId,
                    st.RoleId,
                    Name = st.Staff.Name,
                    Surname = st.Staff.Surname,
                    CategoryId = st.Staff.CategoryId
                })
                .ToList();

            var grouped = links
                .GroupBy(l => l.SportId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // NEW — counts of competitions per sport, one query, no Include needed
            var competitionCounts = _context.Competitions
                .AsNoTracking()
                .GroupBy(c => c.SportId)
                .Select(g => new { SportId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.SportId, x => x.Count);

            Sports = sports.Select(s =>
            {
                var assigned = grouped.TryGetValue(s.Id, out var list) ? list : new();

                var coaches = assigned
                    .Select(a => new CoachItem
                    {
                        StaffId = a.StaffId,
                        FullName = $"{a.Name} {a.Surname}".Trim(),
                        RoleId = a.RoleId,
                        RoleName = ((TeacherSportRole)a.RoleId).ToDisplayName(),
                        CategoryName = ((StaffCategory)a.CategoryId).ToDisplayName()
                    })
                    .OrderBy(c => c.RoleId)
                    .ThenBy(c => c.FullName)
                    .ToList();

                return new SportRow
                {
                    Id = s.Id,
                    Name = s.Name,
                    Coaches = coaches,
                    CompetitionCount = competitionCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0
                };
            }).ToList();
        }
    }
}