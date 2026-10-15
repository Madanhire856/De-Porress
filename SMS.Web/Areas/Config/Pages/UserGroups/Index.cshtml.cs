using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers; 
using SMS.Web.Pages.Shared.Pagination;
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
        public PaginationInfo Pagination { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.UserGroups
                .Include(g => g.Creator)
                .OrderBy(g => g.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(g =>
                    g.Name.Contains(SearchTerm) ||
                    (g.Description != null && g.Description.Contains(SearchTerm)));
            }

            // One-liner pagination
            var result = query.Paginate(PageNumber, PageSize, "user groups");

            UserGroups = result.Items;
            Pagination = result.Pagination;

            // 👇 Apply display numbers to the current page
            _context.ApplyUserGroupNumbers(UserGroups);
        }
    }
}