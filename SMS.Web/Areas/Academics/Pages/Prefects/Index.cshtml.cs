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

namespace SMS.Web.Areas.Academics.Pages.Prefects
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public IndexModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public List<PrefectRow> Prefects { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();

        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> PostTitleOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ClassFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PostTitleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 15;

        public class PrefectRow
        {
            public Guid Id { get; set; }
            public Guid StudentId { get; set; }
            public string StudentNumber { get; set; } = "";
            public string Name { get; set; } = "";
            public string Surname { get; set; } = "";
            public string ClassName { get; set; } = "";
            public string PostTitleName { get; set; } = "";
            public string StatusName { get; set; } = "";
            public string StatusCssClass { get; set; } = "status-active";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public void OnGet()
        {
            if (!_currentUser.HasRight(AccessRights.ViewPrefects))
            {
                // page can still render but list stays empty
                LoadFilterOptions();
                return;
            }

            var query = _context.Prefects
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s.Class)
                .AsQueryable();

            // ---- Filters ----
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(p =>
                    p.Student.Name.ToLower().Contains(term) ||
                    p.Student.Surname.ToLower().Contains(term));
            }

            if (ClassFilter.HasValue)
                query = query.Where(p => p.Student.ClassId == ClassFilter.Value);

            if (PostTitleFilter.HasValue)
                query = query.Where(p => p.PostTitleId == PostTitleFilter.Value);

            if (StatusFilter.HasValue)
                query = query.Where(p => p.StatusId == StatusFilter.Value);

            query = query
                .OrderBy(p => p.Student.Surname)
                .ThenBy(p => p.Student.Name);

            // ---- Paginate ----
            var result = query.Paginate(PageNumber, PageSize, "prefects");
            Pagination = result.Pagination;

            var pageItems = result.Items.ToList();

            // Apply student numbers
            var students = pageItems.Select(p => p.Student).ToList();
            _context.ApplyStudentNumbers(students);

            Prefects = pageItems.Select(p =>
            {
                var status = (PrefectStatus)p.StatusId;
                var postTitle = (PrefectPostTitle)p.PostTitleId;

                return new PrefectRow
                {
                    Id = p.Id,
                    StudentId = p.StudentId,
                    StudentNumber = p.Student.StudentNumber,
                    Name = p.Student.Name,
                    Surname = p.Student.Surname,
                    ClassName = p.Student.Class?.Name ?? "—",
                    PostTitleName = postTitle.ToDisplayName(),
                    StatusName = status.ToDisplayName(),
                    StatusCssClass = status switch
                    {
                        PrefectStatus.ACTIVE => "status-active",
                        PrefectStatus.INACTIVE => "status-inactive",
                        PrefectStatus.DEMOTED => "status-danger",
                        PrefectStatus.COMPLETED => "status-completed",
                        _ => "status-inactive"
                    },
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                };
            }).ToList();

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

            PostTitleOptions = EnumExtensions.AllPrefectPostTitles()
                .Select(p => new SelectListItem
                {
                    Value = ((int)p).ToString(),
                    Text = p.ToDisplayName()
                })
                .ToList();

            StatusOptions = EnumExtensions.AllPrefectStatuses()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToDisplayName()
                })
                .ToList();
        }
    }
}