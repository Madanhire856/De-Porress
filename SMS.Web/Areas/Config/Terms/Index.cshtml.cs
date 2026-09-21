using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Terms
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Term> Terms { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? YearFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Terms
                .OrderByDescending(t => t.AcademicYear)
                .ThenBy(t => t.TermNumber)
                .AsQueryable();

            if (YearFilter.HasValue)
            {
                query = query.Where(t => t.AcademicYear == YearFilter.Value);
            }

            var result = query.Paginate(PageNumber, PageSize, "terms");
            Terms = result.Items;
            Pagination = result.Pagination;

            _context.ApplyTermNumbers(Terms);

            var creatorIds = Terms.Select(t => t.CreatorId).Distinct().ToList();
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