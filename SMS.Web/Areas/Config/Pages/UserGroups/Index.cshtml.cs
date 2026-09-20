using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
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

        public void OnGet()
        {
            var query = _context.UserGroups
                .Include(g => g.Creator)
                .AsQueryable();

            // Apply Search Filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(g =>
                    g.Name.Contains(SearchTerm) ||
                    (g.Description != null && g.Description.Contains(SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                // Example: query = query.Where(g => g.IsActive == (StatusFilter == "Active"));
            }

            UserGroups = query.OrderBy(g => g.Name).ToList();
        }
    }
}