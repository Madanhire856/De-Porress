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

namespace SMS.Web.Areas.Config.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();

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
            var query = _context.Users
                .OrderBy(u => u.Name)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u =>
                    (u.Name != null && u.Name.Contains(SearchTerm)) ||
                    u.Email.Contains(SearchTerm) ||
                    u.LoginId.Contains(SearchTerm));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                bool isActive = StatusFilter == "Active";
                query = query.Where(u => u.IsActive == isActive);
            }

            // Pagination
            var result = query.Paginate(PageNumber, PageSize, "users");

            Users = result.Items;
            Pagination = result.Pagination;

            // Apply generated display numbers
            _context.ApplyUserNumbers(Users);

            // Load creator emails for the current page
            var creatorIds = Users
                .Where(u => u.CreatorId.HasValue)
                .Select(u => u.CreatorId!.Value)
                .Distinct()
                .ToList();

            if (creatorIds.Any())
            {
                CreatorEmails = _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Email);
            }
        }
    }
}