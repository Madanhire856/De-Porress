using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
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
            var query = _context.Staff
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s =>
                    s.Name.Contains(SearchTerm) ||
                    s.Surname.Contains(SearchTerm) ||
                    s.EcNumber.Contains(SearchTerm) ||
                    s.IdNumber.Contains(SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                bool isActive = StatusFilter == "Active";
                query = query.Where(s => s.IsActive == isActive);
            }

            var result = query.Paginate(PageNumber, PageSize, "staff members");

            Staff = result.Items;
            Pagination = result.Pagination;
        }
    }
}