using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;

namespace SMS.Web.Areas.Academics.Pages.Prefects;

[Authorize]
public class BoardModel : PageModel
{
    private readonly SMSDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BoardModel(SMSDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public List<PrefectBoardCard> AllPrefects { get; private set; } = [];
    public int ActiveCount => AllPrefects.Count(p => p.Status == PrefectStatus.ACTIVE);
    public int CompletedCount => AllPrefects.Count(p => p.Status == PrefectStatus.COMPLETED);

    public async Task OnGetAsync()
    {
        if (!_currentUser.HasRight(AccessRights.ViewPrefects))
            return;

        var prefects = await _context.Prefects
            .AsNoTracking()
            .Include(p => p.Student)
                .ThenInclude(s => s.Class)
            .OrderBy(p => p.Student.Surname)
            .ThenBy(p => p.Student.Name)
            .ToListAsync();

        _context.ApplyStudentNumbers(prefects.Select(p => p.Student).ToList());

        var cards = prefects.Select(p => new PrefectBoardCard
        {
            Id = p.Id,
            StudentName = $"{p.Student.Name} {p.Student.Surname}".Trim(),
            StudentPhotoUrl = p.Student.PhotoUrl,
            StudentNumber = p.Student.StudentNumber,
            ClassName = p.Student.Class?.Name ?? "No class",
            PostTitle = Enum.IsDefined(typeof(PrefectPostTitle), p.PostTitleId)
                ? ((PrefectPostTitle)p.PostTitleId).ToDisplayName()
                : "Unknown position",
            Status = Enum.IsDefined(typeof(PrefectStatus), p.StatusId)
                ? (PrefectStatus)p.StatusId
                : PrefectStatus.INACTIVE,
            StartDate = p.StartDate,
            EndDate = p.EndDate
        }).ToList();

        AllPrefects = cards;
    }

    public sealed class PrefectBoardCard
    {
        public Guid Id { get; init; }
        public string StudentName { get; init; } = "";
        public string StudentNumber { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string PostTitle { get; init; } = "";
        public PrefectStatus Status { get; init; }
        public string StatusCssClass => Status switch
        {
            PrefectStatus.ACTIVE => "status-active",
            PrefectStatus.INACTIVE => "status-inactive",
            PrefectStatus.DEMOTED => "status-danger",
            PrefectStatus.COMPLETED => "status-completed",
            _ => "status-inactive"
        };
        public string? StudentPhotoUrl { get; init; }
        public string StatusName => Status.ToDisplayName();
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        public string Initials => string.Join("", StudentName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(name => char.ToUpperInvariant(name[0])));
    }

}
