using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Grades
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Grade> Grades { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> CreatorEmails { get; set; } = new();
        public Dictionary<Guid, int> ClassCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GradeLevelFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
            var query = _context.Grades
                .OrderBy(g => g.Name)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(g => g.Name.Contains(SearchTerm));
            }

            if (GradeLevelFilter.HasValue)
            {
                query = query.Where(g => g.GradeLevelId == GradeLevelFilter.Value);
            }

            var result = query.Paginate(PageNumber, PageSize, "grades");
            Grades = result.Items;
            Pagination = result.Pagination;

            _context.ApplyGradeNumbers(Grades);

            var creatorIds = Grades.Select(g => g.CreatorId).Distinct().ToList();
            if (creatorIds.Any())
            {
                CreatorEmails = _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Email);
            }

            var gradeIds = Grades.Select(g => g.Id).ToList();
            if (gradeIds.Any())
            {
                ClassCounts = _context.Classes
                    .AsNoTracking()
                    .Where(c => gradeIds.Contains(c.GradeId))
                    .GroupBy(c => c.GradeId)
                    .Select(g => new { GradeId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.GradeId, x => x.Count);
            }
        }
    }
}