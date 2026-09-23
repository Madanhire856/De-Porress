using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Academics.Pages.Students
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;

        public IndexModel(SMSDbContext context)
        {
            _context = context;
        }

        public List<StudentRow> Students { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();

        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> GenderOptions { get; set; } = new();
        public List<SelectListItem> EnrolmentStatusOptions { get; set; } = new();

        public string? VillageFilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ClassFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GenderFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EnrolmentFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? VillageFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 15;

        public class StudentRow
        {
            public Guid Id { get; set; }
            public string StudentNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Surname { get; set; } = "";
            public DateTime? Dob { get; set; }
            public int GenderId { get; set; }
            public string GenderName { get; set; } = "";
            public string ClassName { get; set; } = "";
            public string VillageName { get; set; } = "";
            public string EnrolmentStatusName { get; set; } = "";
            public DateTime? EnrolmentDate { get; set; }
        }

        public void OnGet()
        {
            var query = _context.Students
                .AsNoTracking()
                .Include(s => s.Class)
                .Include(s => s.Village)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    s.Surname.ToLower().Contains(term));
            }

            if (ClassFilter.HasValue)
                query = query.Where(s => s.ClassId == ClassFilter.Value);

            if (GenderFilter.HasValue)
                query = query.Where(s => s.GenderId == GenderFilter.Value);

            if (EnrolmentFilter.HasValue)
                query = query.Where(s => s.EnrolmentStatusId == EnrolmentFilter.Value);

            if (VillageFilter.HasValue)
                query = query.Where(s => s.VillageId == VillageFilter.Value);

            query = query
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name);

            var result = query.Paginate(PageNumber, PageSize, "students");
            Pagination = result.Pagination;

            // Materialize the page so the sequence extension can mutate in place
            var pageItems = result.Items.ToList();
            _context.ApplyStudentNumbers(pageItems);

            Students = pageItems.Select(s => new StudentRow
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                Name = s.Name,
                Surname = s.Surname,
                FullName = $"{s.Name} {s.Surname}".Trim(),
                Dob = s.Dob,
                GenderId = s.GenderId,
                GenderName = ((Gender)s.GenderId).ToDisplayName(),
                ClassName = s.Class != null ? s.Class.Name : "—",
                VillageName = s.Village != null ? s.Village.Name : "—",
                EnrolmentStatusName = ((EnrolmentStatus)s.EnrolmentStatusId).ToDisplayName(),
                EnrolmentDate = s.EnrolmentDate
            }).ToList();

            // Lookup the village name for the banner
            if (VillageFilter.HasValue)
            {
                VillageFilterName = _context.Villages
                    .AsNoTracking()
                    .Where(v => v.Id == VillageFilter.Value)
                    .Select(v => v.Name)
                    .FirstOrDefault();
            }

            LoadFilterOptions();
        }

        private void LoadFilterOptions()
        {
            ClassOptions = _context.Classes
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            GenderOptions = EnumExtensions.AllGenders()
                .Select(g => new SelectListItem
                {
                    Value = ((int)g).ToString(),
                    Text = g.ToDisplayName()
                })
                .ToList();

            EnrolmentStatusOptions = EnumExtensions.AllEnrolmentStatuses()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToDisplayName()
                })
                .ToList();
        }
    }
}