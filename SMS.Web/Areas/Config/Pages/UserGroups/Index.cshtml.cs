using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.UserGroups
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<UserGroup> UserGroups { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        // --- PAGINATION ---
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 50;

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public void OnGet()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;

            var query = _context.UserGroups
                .Include(g => g.Creator)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(g =>
                    g.Name.Contains(SearchTerm) ||
                    (g.Description != null && g.Description.Contains(SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                // query = query.Where(g => g.IsActive == (StatusFilter == "Active"));
            }

            // Total counts
            TotalCount = query.Count();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

            // Apply paging
            UserGroups = query
                .OrderBy(g => g.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}