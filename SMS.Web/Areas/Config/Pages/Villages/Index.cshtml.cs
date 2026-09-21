using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Villages  
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Village> Villages { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();
        public Dictionary<Guid, int> StudentCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Villages
                .OrderBy(v => v.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(v =>
                    v.Name.Contains(SearchTerm) ||
                    v.Headman.Contains(SearchTerm) ||
                    v.Chief.Contains(SearchTerm));
            }

            var result = query.Paginate(PageNumber, PageSize, "villages");
            Villages = result.Items;
            Pagination = result.Pagination;

            _context.ApplyVillageNumbers(Villages);

            // Creator emails
            var creatorIds = Villages.Select(v => v.CreatorId).Distinct().ToList();
            if (creatorIds.Any())
            {
                CreatorEmails = _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Email);
            }

            // Student counts per village
            var villageIds = Villages.Select(v => v.Id).ToList();
            if (villageIds.Any())
            {
                StudentCounts = _context.Students
                    .AsNoTracking()
                    .Where(s => s.VillageId.HasValue && villageIds.Contains(s.VillageId.Value))
                    .GroupBy(s => s.VillageId!.Value)
                    .Select(g => new { VillageId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.VillageId, x => x.Count);
            }
        }
    }
}