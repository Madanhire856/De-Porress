using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Academics.Pages.Staff
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Data.Staff> Staff { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public Dictionary<Guid, int> SubjectCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            // Category dropdown options
            CategoryOptions = EnumExtensions.AllStaffCategories()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToDisplayName()
                })
                .ToList();

            var query = _context.Staff
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s =>
                    s.Name.Contains(SearchTerm) ||
                    s.Surname.Contains(SearchTerm) ||
                    s.EcNumber.Contains(SearchTerm) ||
                    s.IdNumber.Contains(SearchTerm));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                bool isActive = StatusFilter == "Active";
                query = query.Where(s => s.IsActive == isActive);
            }

            // Category filter
            if (CategoryFilter.HasValue)
            {
                query = query.Where(s => s.CategoryId == CategoryFilter.Value);
            }

            // Pagination
            var result = query.Paginate(PageNumber, PageSize, "staff members");

            Staff = result.Items;
            Pagination = result.Pagination;

            // Subject counts for the current page
            var staffIds = Staff.Select(s => s.Id).ToList();
            if (staffIds.Any())
            {
                SubjectCounts = _context.TeacherSubjects
                    .AsNoTracking()
                    .Where(ts => staffIds.Contains(ts.StaffId))
                    .GroupBy(ts => ts.StaffId)
                    .Select(g => new { StaffId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.StaffId, x => x.Count);
            }
        }
    }
}