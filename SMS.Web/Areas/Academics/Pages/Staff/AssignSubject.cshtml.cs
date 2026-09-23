using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Academics.Pages.Staff
{
    [Authorize]
    public class AssignSubjectModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AssignSubjectModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public Data.Staff StaffVM { get; set; } = null!;
        public List<SubjectGroup> SubjectsByCategory { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedSubjectIds { get; set; } = new();

        public class SubjectGroup
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public string CategorySlug { get; set; } = "";
            public List<SubjectItem> Subjects { get; set; } = new();
        }

        public class SubjectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(Guid staffId)
        {
            // ---- Rights guard ----
            if (!_currentUser.HasRight(AccessRights.EditSubjects))
                return Forbid();

            StaffVM = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (StaffVM == null)
                return NotFound();

            // Load current assignments
            SelectedSubjectIds = await _context.TeacherSubjects
                .AsNoTracking()
                .Where(ts => ts.StaffId == staffId)
                .Select(ts => ts.SubjectId)
                .ToListAsync();

            await LoadSubjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid staffId)
        {
            // ---- Same guard on POST ----
            if (!_currentUser.HasRight(AccessRights.EditSubjects))
                return Forbid();

            StaffVM = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (StaffVM == null)
                return NotFound();

            // Sync TeacherSubject rows: delete-all, insert-selected
            var existing = await _context.TeacherSubjects
                .Where(ts => ts.StaffId == staffId)
                .ToListAsync();

            _context.TeacherSubjects.RemoveRange(existing);

            var validSubjectIds = SelectedSubjectIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            foreach (var subjectId in validSubjectIds)
            {
                _context.TeacherSubjects.Add(new TeacherSubject
                {
                    Id = Guid.NewGuid(),
                    StaffId = staffId,
                    SubjectId = subjectId
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = staffId });
        }

        private async Task LoadSubjectsAsync()
        {
            var allSubjects = await _context.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.CategoryId
                })
                .ToListAsync();

            SubjectsByCategory = allSubjects
                .GroupBy(s => s.CategoryId)
                .OrderBy(g => g.Key)
                .Select(g => new SubjectGroup
                {
                    CategoryId = g.Key,
                    CategoryName = ((SubjectCategory)g.Key).ToDisplayName(),
                    CategorySlug = "cat-" + g.Key,
                    Subjects = g.Select(s => new SubjectItem
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code
                    }).ToList()
                })
                .ToList();
        }
    }
}