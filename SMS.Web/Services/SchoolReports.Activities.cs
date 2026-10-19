using Microsoft.EntityFrameworkCore;
using SMS.Lib;

namespace SMS.Web.Services;

public sealed partial class SchoolReportService
{
    private async Task<ReportTable> Trips(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(SchoolTrips.Visible(db, SchoolTrips.CanViewDrafts(access)).AsNoTracking(), x => x.DepartureAt, f);
        if (f.GradeId.HasValue) q = q.Where(x => x.Grades.Any(g => g.GradeId == f.GradeId));
        if (f.Status.HasValue) q = q.Where(x => x.StatusId == f.Status);
        var rows = await Load(q.Include(x => x.Leader).Include(x => x.Grades).ThenInclude(x => x.Grade).OrderBy(x => x.DepartureAt).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Trip", "Destination", "Leader", "Grades", "Departure (Zimbabwe)", "Return (Zimbabwe)", "Days", "Nights", "Currency", "Per student", "Payment due", "Status"]);
        foreach (var x in rows) result.Add(x.Name, x.Destination, StaffName(x.Leader), string.Join(", ", x.Grades.OrderBy(g => g.Grade.GradeLevelId).ThenBy(g => g.Grade.Name).Select(g => g.Grade.Name)), DateTimeText(x.DepartureAt), DateTimeText(x.ReturnAt), x.Days.ToString(), x.Nights.ToString(), x.CurrencyCode, Money(x.AmountPerStudent), x.PaymentDueDate?.ToString("yyyy-MM-dd") ?? "—", SchoolTrips.StatusName(x.StatusId));
        result.Notes.Add("Trip costs are planned per-student amounts, not amounts collected. There are no trip participant/payment records to report yet.");
        if (!SchoolTrips.CanViewDrafts(access)) result.Notes.Add("Draft trips are excluded.");
        return result;
    }
    private async Task<ReportTable> Masses(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(db.MassEvents.AsNoTracking(), x => x.DateTime, f);
        var rows = await Load(q.OrderBy(x => x.DateTime).ThenBy(x => x.Id).Select(x => new {
            x.DateTime, x.LiturgicalDate, x.Venue, x.Celebrant, Entries = x.MassRosters.Count,
            Students = x.MassRosters.Where(r => r.StudentId.HasValue).Select(r => r.StudentId).Distinct().Count(),
            Staff = x.MassRosters.Where(r => r.StaffId.HasValue).Select(r => r.StaffId).Distinct().Count()
        }), ct);
        var result = new ReportTable(["Mass (Zimbabwe)", "Liturgical date", "Venue", "Celebrant", "Roster entries", "Students assigned", "Staff assigned"]);
        foreach (var x in rows) result.Add(DateTimeText(x.DateTime), x.LiturgicalDate.ToString("yyyy-MM-dd"), x.Venue, x.Celebrant, x.Entries.ToString(), x.Students.ToString(), x.Staff.ToString());
        result.Notes.Add("Roster assignments do not establish attendance at Mass.");
        return result;
    }
    private async Task<ReportTable> Prefects(ReportFilters f, CancellationToken ct)
    {
        var studentIds = Students(f).Select(x => x.Id);
        var q = Dates(db.Prefects.AsNoTracking().Where(x => studentIds.Contains(x.StudentId)), x => x.StartDate, f);
        if (f.Status.HasValue) q = q.Where(x => x.StatusId == f.Status);
        var rows = await Load(q.Include(x => x.Student).ThenInclude(x => x.Class).OrderBy(x => x.PostTitleId).ThenBy(x => x.Student.Surname).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Learner", "Current class", "Post", "Start", "End", "Status"]);
        foreach (var x in rows) result.Add(Person(x.Student), Text(x.Student.Class?.Name), ReportCatalog.Name<PrefectPostTitle>(x.PostTitleId), Date(x.StartDate), Date(x.EndDate), ReportCatalog.Name<PrefectStatus>(x.StatusId));
        return result;
    }
    private async Task<ReportTable> Nominations(ReportFilters f, CancellationToken ct)
    {
        var studentIds = Students(f).Select(x => x.Id);
        var q = Dates(db.PrefectNominations.AsNoTracking().Where(x => studentIds.Contains(x.StudentId)), x => x.CreationDate, f, true);
        if (access.Workspace == SchoolWorkspace.Teacher) q = q.Where(x => user.UserId.HasValue && x.NominatedByUserId == user.UserId);
        if (f.Status.HasValue) q = q.Where(x => x.StatusId == f.Status);
        var rows = await Load(q.Include(x => x.Student).ThenInclude(x => x.Class).OrderByDescending(x => x.CreationDate).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Learner", "Current class", "Proposed post", "Nominated (Zimbabwe)", "Status", "Reviewed (Zimbabwe)"]);
        foreach (var x in rows) result.Add(Person(x.Student), Text(x.Student.Class?.Name), x.PostTitleId.HasValue ? ReportCatalog.Name<PrefectPostTitle>(x.PostTitleId.Value) : "—", DateTimeText(x.CreationDate.AddHours(2)), ReportCatalog.Name<PrefectAppointmentStatus>(x.StatusId), x.ReviewedAt.HasValue ? DateTimeText(x.ReviewedAt.Value.AddHours(2)) : "—");
        if (access.Workspace == SchoolWorkspace.Teacher) result.Notes.Add("Limited to your own nominations for learners in your current assigned classes.");
        return result;
    }
    private async Task<ReportTable> Sdc(ReportFilters f, CancellationToken ct)
    {
        var q = db.SdcMembers.AsNoTracking();
        if (f.TermId.HasValue) q = q.Where(x => x.TermId == f.TermId);
        if (f.Status.HasValue) q = q.Where(x => x.IsActive == (f.Status == 1));
        var rows = await Load(q.Include(x => x.Position).Include(x => x.Term).OrderByDescending(x => x.Term.StartDate).ThenBy(x => x.Position.Name).ThenBy(x => x.Name).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Member", "Position", "Committee term", "Term start", "Term end", "Member status", "Term status"]);
        foreach (var x in rows) result.Add(x.Name, x.Position.Name, x.Term.Name, Date(x.Term.StartDate), Date(x.Term.EndDate), x.IsActive ? "Active" : "Inactive", x.Term.IsActive ? "Active" : "Inactive");
        return result;
    }
    private async Task<ReportTable> Notices(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(SchoolNotices.Managed(db).AsNoTracking(), x => x.CreationDate, f, true);
        if (f.Status.HasValue) q = q.Where(x => x.StatusId == f.Status);
        var rows = await Load(q.OrderByDescending(x => x.CreationDate).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Title", "Audience", "Publication status", "Created (Zimbabwe)"]);
        foreach (var x in rows) result.Add(x.Title, x.ChannelId == (int)AnnouncementChannel.STAFF_NOTICE ? "Staff only" : "Public website", ReportCatalog.Statuses("notices").GetValueOrDefault(x.StatusId, $"Unknown ({x.StatusId})"), DateTimeText(x.CreationDate.AddHours(2)));
        result.Notes.Add("Publication status does not prove that a notice has been read or delivered by email.");
        return result;
    }
    private async Task<ReportTable> Houses(CancellationToken ct)
    {
        var rows = await Load(db.Houses.AsNoTracking().Include(x => x.HouseTeachers).ThenInclude(x => x.Staff).OrderBy(x => x.Name).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["House", "Colour", "Staff assignments"]);
        foreach (var x in rows) result.Add(x.Name, x.Color, string.Join("; ", x.HouseTeachers.OrderBy(y => y.RoleId).ThenBy(y => y.Staff.Surname).Select(y => StaffName(y.Staff) + " (" + ReportCatalog.Name<TeacherHouseRole>(y.RoleId) + ")")));
        return result;
    }
    private async Task<ReportTable> Sports(CancellationToken ct)
    {
        var rows = await Load(db.Sports.AsNoTracking().Include(x => x.SportTeachers).ThenInclude(x => x.Staff).OrderBy(x => x.Name).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Sport", "Coaching assignments"]);
        foreach (var x in rows) result.Add(x.Name, string.Join("; ", x.SportTeachers.OrderBy(y => y.RoleId).ThenBy(y => y.Staff.Surname).Select(y => StaffName(y.Staff) + " (" + ReportCatalog.Name<TeacherSportRole>(y.RoleId) + ")")));
        return result;
    }
}
