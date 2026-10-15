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

namespace SMS.Web.Areas.Config.Pages.Houses
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<House> Houses { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();
        public Dictionary<Guid, TeacherSummary> TeacherSummaries { get; set; } = new();

        public class TeacherSummary
        {
            public string MasterName { get; set; } = "";
            public int TotalCount { get; set; }
            public int AdditionalCount => Math.Max(0, TotalCount - (string.IsNullOrEmpty(MasterName) ? 0 : 1));
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Houses
                .OrderBy(h => h.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(h => h.Name.Contains(SearchTerm));
            }

            var result = query.Paginate(PageNumber, PageSize, "houses");
            Houses = result.Items;
            Pagination = result.Pagination;

            _context.ApplyHouseNumbers(Houses);

            // Creator emails
            var creatorIds = Houses.Select(h => h.CreatorId).Distinct().ToList();
            if (creatorIds.Any())
            {
                CreatorEmails = _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Email);
            }

            // 👇 Load assigned teachers for the current page
            var houseIds = Houses.Select(h => h.Id).ToList();
            if (houseIds.Any())
            {
                var links = _context.HouseTeachers
                    .AsNoTracking()
                    .Where(ht => houseIds.Contains(ht.HouseId))
                    .OrderBy(ht => ht.RoleId)
                    .ThenBy(ht => ht.Staff.Surname)
                    .Select(ht => new
                    {
                        ht.HouseId,
                        ht.RoleId,
                        StaffName = ht.Staff.Name + " " + ht.Staff.Surname
                    })
                    .ToList();

                TeacherSummaries = links
                    .GroupBy(l => l.HouseId)
                    .ToDictionary(
                        g => g.Key,
                        g => new TeacherSummary
                        {
                            MasterName = g.FirstOrDefault(x => x.RoleId == (int)TeacherHouseRole.MASTER)?.StaffName ?? "",
                            TotalCount = g.Count()
                        });
            }
        }
    }
}