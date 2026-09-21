using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;      // 👈 added for SelectListItem
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Classes
{
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<Data.Class> Classes { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public Dictionary<Guid, string> TeacherNames { get; set; } = new();
        public Dictionary<Guid, string> GradeNames { get; set; } = new();
        public Dictionary<Guid, int> StudentCounts { get; set; } = new();
        public List<SelectListItem> GradeOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? GradeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public void OnGet()
        {
  
            GradeOptions = _context.Grades
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.Name
                })
                .ToList();

            var query = _context.Classes
                .OrderBy(c => c.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(c => c.Name.Contains(SearchTerm));
            }

            if (GradeFilter.HasValue)
            {
                query = query.Where(c => c.GradeId == GradeFilter.Value);
            }

            var result = query.Paginate(PageNumber, PageSize, "classes");
            Classes = result.Items;
            Pagination = result.Pagination;

            _context.ApplyClassNumbers(Classes);

            // --- Teacher names for the current page ---
            var teacherIds = Classes.Select(c => c.ClassTeacherId).Distinct().ToList();
            if (teacherIds.Any())
            {
                TeacherNames = _context.Staff
                    .AsNoTracking()
                    .Where(s => teacherIds.Contains(s.Id))
                    .ToDictionary(s => s.Id, s => $"{s.Name} {s.Surname}");
            }

            var gradeIds = Classes.Select(c => c.GradeId).Distinct().ToList();
            if (gradeIds.Any())
            {
                GradeNames = _context.Grades
                    .AsNoTracking()
                    .Where(g => gradeIds.Contains(g.Id))
                    .ToDictionary(g => g.Id, g => g.Name);
            }

            var classIds = Classes.Select(c => c.Id).ToList();
            if (classIds.Any())
            {
                StudentCounts = _context.Students
                    .AsNoTracking()
                    .Where(s => s.ClassId.HasValue && classIds.Contains(s.ClassId.Value))
                    .GroupBy(s => s.ClassId!.Value)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.ClassId, x => x.Count);
            }
        }
    }
}