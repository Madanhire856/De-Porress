using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Subjects
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Subject> Subjects { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Subjects
                .OrderBy(s => s.Name)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s =>
                    s.Name.Contains(SearchTerm) ||
                    s.Code.Contains(SearchTerm));
            }

            // Category filter
            if (CategoryFilter.HasValue)
            {
                query = query.Where(s => s.CategoryId == CategoryFilter.Value);
            }

            var result = query.Paginate(PageNumber, PageSize, "subjects");

            Subjects = result.Items;
            Pagination = result.Pagination;

 
            _context.ApplySubjectNumbers(Subjects);

            var creatorIds = Subjects
                .Select(s => s.CreatorId)
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