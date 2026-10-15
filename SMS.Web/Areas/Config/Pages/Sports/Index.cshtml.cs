using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Sports
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Sport> Sports { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();
        public Dictionary<Guid, TeacherSummary> TeacherSummaries { get; set; } = new();

        public class TeacherSummary
        {
            public string CoachName { get; set; } = "";
            public int TotalCount { get; set; }
            public int AdditionalCount => Math.Max(0, TotalCount - (string.IsNullOrEmpty(CoachName) ? 0 : 1));
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Sports
                .OrderBy(s => s.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s => s.Name.Contains(SearchTerm));
            }

            var result = query.Paginate(PageNumber, PageSize, "sports");
            Sports = result.Items;
            Pagination = result.Pagination;

            _context.ApplySportNumbers(Sports);

            // Creator emails
            var creatorIds = Sports.Select(s => s.CreatorId).Distinct().ToList();
            if (creatorIds.Any())
            {
                CreatorEmails = _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Email);
            }

            // Assigned coaches for the current page
            var sportIds = Sports.Select(s => s.Id).ToList();
            if (sportIds.Any())
            {
                var links = _context.SportTeachers
                    .AsNoTracking()
                    .Where(st => sportIds.Contains(st.SportId))
                    .OrderBy(st => st.RoleId)
                    .ThenBy(st => st.Staff.Surname)
                    .Select(st => new
                    {
                        st.SportId,
                        st.RoleId,
                        StaffName = st.Staff.Name + " " + st.Staff.Surname
                    })
                    .ToList();

                TeacherSummaries = links
                    .GroupBy(l => l.SportId)
                    .ToDictionary(
                        g => g.Key,
                        g => new TeacherSummary
                        {
                            CoachName = g.FirstOrDefault(x => x.RoleId == (int)TeacherSportRole.HEAD_COACH)?.StaffName ?? "",
                            TotalCount = g.Count()
                        });
            }
        }
    }
}