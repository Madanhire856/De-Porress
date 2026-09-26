using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Prefects.ViewModels;
using SMS.Web.Pages.GeneratedNumbers;

namespace SMS.Web.Areas.Academics.Pages.Prefects
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpsertModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty]
        public PrefectVM Input { get; set; } = new();

        public List<SelectListItem> StudentOptions { get; private set; } = new();
        public List<SelectListItem> PostTitleOptions { get; private set; } = new();
        public List<SelectListItem> StatusOptions { get; private set; } = new();
        public List<SelectListItem> NominatorOptions { get; private set; } = new();

        // Single source of truth for create vs. edit.
        public bool IsEdit => Input.Id != Guid.Empty;

        // ============================================================
        //  GET
        // ============================================================
        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            await LoadDropdownsAsync();

            if (id.HasValue && id.Value != Guid.Empty)
            {
                var prefect = await _context.Prefects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id.Value);

                if (prefect == null)
                    return NotFound();

                Input = new PrefectVM
                {
                    Id = prefect.Id,
                    StudentId = prefect.StudentId,
                    PostTitleId = prefect.PostTitleId,
                    StatusId = prefect.StatusId,
                    StartDate = prefect.StartDate,
                    EndDate = prefect.EndDate,
                    NominatorId = prefect.NominatorId
                };
            }
            else
            {
                // Create mode: leave Id as Guid.Empty.
                Input = new PrefectVM();
            }

            return Page();
        }

        // ============================================================
        //  POST
        // ============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            // EndDate must not be before StartDate
            if (Input.EndDate.HasValue && Input.EndDate.Value.Date < Input.StartDate.Date)
            {
                ModelState.AddModelError("Input.EndDate",
                    "End date must be on or after the start date.");
                await LoadDropdownsAsync();
                return Page();
            }

            // Student must exist
            var studentExists = await _context.Students
                .AsNoTracking()
                .AnyAsync(s => s.Id == Input.StudentId);

            if (!studentExists)
            {
                ModelState.AddModelError("Input.StudentId",
                    "The selected student could not be found.");
                await LoadDropdownsAsync();
                return Page();
            }

            // ========================================================
            //  BUSINESS RULE: one prefect may hold only one post
            //  at a time — reject overlapping ACTIVE appointments.
            // ========================================================
            var newStart = Input.StartDate.Date;
            var newEnd = Input.EndDate!.Value.Date;

            var conflict = await _context.Prefects
                .AsNoTracking()
                .Where(p => p.StudentId == Input.StudentId)
                .Where(p => p.Id != Input.Id)                          // exclude self when editing
                .Where(p => p.StatusId == (int)PrefectStatus.ACTIVE)   // only live posts block
                .Where(p => p.StartDate <= newEnd && p.EndDate >= newStart) // overlap
                .Select(p => new { p.PostTitleId, p.StartDate, p.EndDate })
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                var title = Enum.IsDefined(typeof(PrefectPostTitle), conflict.PostTitleId)
                    ? ((PrefectPostTitle)conflict.PostTitleId).ToDisplayName()
                    : conflict.PostTitleId.ToString();

                ModelState.AddModelError("Input.StudentId",
                    $"This student already holds the post of “{title}” "
                  + $"from {conflict.StartDate:yyyy-MM-dd} to {conflict.EndDate:yyyy-MM-dd}. "
                  + "A prefect can only hold one post at a time.");

                await LoadDropdownsAsync();
                return Page();
            }

            // Enum sanity checks
            if (!Enum.IsDefined(typeof(PrefectPostTitle), Input.PostTitleId))
                ModelState.AddModelError("Input.PostTitleId", "Invalid post title.");

            if (!Enum.IsDefined(typeof(PrefectStatus), Input.StatusId))
                ModelState.AddModelError("Input.StatusId", "Invalid status.");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            if (IsEdit)
            {
                // ---------- UPDATE ----------
                var entity = await _context.Prefects
                    .FirstOrDefaultAsync(p => p.Id == Input.Id);

                if (entity == null)
                    return NotFound();

                entity.StudentId = Input.StudentId;
                entity.PostTitleId = Input.PostTitleId;
                entity.StatusId = Input.StatusId;
                entity.StartDate = Input.StartDate;
                entity.EndDate = Input.EndDate!.Value;
                entity.NominatorId = Input.NominatorId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prefect appointment updated successfully.";
            }
            else
            {
                // ---------- CREATE ----------
                var entity = new Prefect
                {
                    Id = Guid.NewGuid(),
                    StudentId = Input.StudentId,
                    PostTitleId = Input.PostTitleId,
                    StatusId = Input.StatusId,
                    StartDate = Input.StartDate,
                    EndDate = Input.EndDate!.Value,
                    NominatorId = Input.NominatorId,
                    CreationDate = DateTime.Now,
                    CreatorId = _currentUser.UserId
                };

                _context.Prefects.Add(entity);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Prefect appointment created successfully.";
            }

            return RedirectToPage("./Index");
        }

        // ============================================================
        //  DROPDOWNS
        // ============================================================
        private async Task LoadDropdownsAsync()
        {
            // Students
            var students = await _context.Students
                .AsNoTracking()
                .Include(s => s.Class)
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync();

            _context.ApplyStudentNumbers(students);

            StudentOptions = students.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Surname}, {s.Name} ({s.StudentNumber})"
                      + (s.Class != null ? " – " + s.Class.Name : "")
            }).ToList();

            // ---- UX hint: mark students who currently hold a post ----
            // (Purely cosmetic; the server-side overlap check above is what
            //  actually enforces the rule.)
            var today = DateTime.Today;

            var busyIds = await _context.Prefects
                .AsNoTracking()
                .Where(p => p.StatusId == (int)PrefectStatus.ACTIVE
                         && p.StartDate <= today
                         && p.EndDate >= today)
                .Select(p => p.StudentId)
                .Distinct()
                .ToListAsync();

            if (busyIds.Count > 0)
            {
                var busy = busyIds.ToHashSet();

                foreach (var item in StudentOptions)
                {
                    if (Guid.TryParse(item.Value, out var sid)
                        && busy.Contains(sid)
                        && sid != Input.StudentId)   // keep currently-edited student selectable
                    {
                        item.Text += "  (already holds a post)";
                    }
                }
            }

            // Post titles
            PostTitleOptions = EnumExtensions.AllPrefectPostTitles()
                .Select(p => new SelectListItem
                {
                    Value = ((int)p).ToString(),
                    Text = p.ToDisplayName()
                }).ToList();

            // Statuses
            StatusOptions = EnumExtensions.AllPrefectStatuses()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToDisplayName()
                }).ToList();

            // Nominators (staff)
            var staff = await _context.Staff
                .AsNoTracking()
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync();

            NominatorOptions = staff.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Surname}, {s.Name}"
            }).ToList();
        }
    }
}