using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;

namespace SMS.Web.Services;

[Flags] public enum ReportFields { None = 0, Dates = 1, Grade = 2, Class = 4, Term = 8, Status = 16 }
public record ReportDefinition(string Key, string Title, string Group, string Description, AccessRights Rights, ReportFields Fields, params SchoolWorkspace[] Workspaces) { public FeatureRights FeatureRight { get; init; } }
public class ReportFilters
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? TermId { get; set; }
    public int? Status { get; set; }
}
public record ReportMetric(string Label, string Value);
public sealed class ReportTable(string[] headers)
{
    public string[] Headers { get; } = headers;
    public List<string[]> Rows { get; } = [];
    public List<ReportMetric> Metrics { get; } = [];
    public List<string> Notes { get; } = [];
    public void Add(params string[] cells) => Rows.Add(cells);
}
public sealed class ReportLimitException() : Exception("This report exceeds 25,000 records. Narrow the filters before viewing, printing or exporting.");

public static class ReportCatalog
{
    private const SchoolWorkspace A = SchoolWorkspace.Administration, T = SchoolWorkspace.Teacher, B = SchoolWorkspace.Bursar;
    private const ReportFields D = ReportFields.Dates, G = ReportFields.Grade, C = ReportFields.Class, P = ReportFields.Term, S = ReportFields.Status;
    public static IReadOnlyList<ReportDefinition> All { get; } = [
        new("learners", "Learner register", "Learners", "Enrolment, class placement and learner status. Date filters use enrolment dates.", AccessRights.ViewLearners, D|G|C|S, A,T),
        new("attendance", "Attendance register", "Learners", "Recorded attendance by learner, class and date. Unrecorded days are not counted as absences.", AccessRights.ViewLearners, D|G|C|S, A,T),
        new("staff", "Staff register", "Staffing", "Staff categories, active status and joining dates. Personal identity numbers are excluded.", AccessRights.ViewStaff, D|S, A,T),
        new("class-teachers", "Class teacher assignments", "Staffing", "Class teacher assignment history. Date filters use assignment dates.", AccessRights.ViewClasses|AccessRights.ViewStaff, D|G|C|S, A,T),
        new("subjects", "Subject assignments", "Staffing", "Subjects and assigned teaching staff.", AccessRights.ViewSubjects|AccessRights.ViewStaff, ReportFields.None, A,T),
        new("balances", "Fee balances", "Finance", "Current ledger balances by term and currency, with outstanding and credit totals kept separate.", AccessRights.ViewFeeReports|AccessRights.ViewFeeBalances, G|C|P|S, A,B),
        new("collections", "Collections and reversals", "Finance", "Receipts by payment date. Cash-up permissions control which bursars' records are included.", AccessRights.ViewFeeReports|AccessRights.ViewFeeBalances, D|G|C|P|S, A,B),
        new("fee-structures", "Fee structures", "Finance", "Configured charges by grade, term and currency.", AccessRights.ViewFeeReports|AccessRights.ViewFeeBalances, G|P, A,B),
        new("trips", "Trip plans", "School activities", "Destinations, grades, leaders, costs and travel dates. Date filters use departure dates.", AccessRights.None, D|G|S, A,T,B) { FeatureRight = FeatureRights.ViewTrips },
        new("masses", "Mass timetable", "School activities", "Mass dates, venues, celebrants and ministry assignment counts.", AccessRights.None, D, A,T) { FeatureRight = FeatureRights.ViewMasses },
        new("prefects", "Prefect appointments", "Learners", "Prefect posts, service periods and status. Date filters use appointment start dates.", AccessRights.ViewPrefects|AccessRights.ViewLearners, D|G|C|S, A,T),
        new("nominations", "Prefect nominations", "Learners", "Nomination and review status. Teachers see only their own nominations.", AccessRights.ViewPrefects|AccessRights.ViewLearners, D|G|C|S, A,T),
        new("sdc", "SDC membership", "Administration", "Committee membership, positions, committee terms and active status.", AccessRights.None, P|S, A) { FeatureRight = FeatureRights.ViewSdcMembers },
        new("notices", "Notice publication", "Administration", "Public and staff-only notice titles, audiences and publication status. Dates use Zimbabwe time.", AccessRights.None, D|S, A) { FeatureRight = FeatureRights.ViewNotices },
        new("houses", "House assignments", "School activities", "Configured houses and assigned staff.", AccessRights.ViewHouses, ReportFields.None, A,T),
        new("sports", "Sports coaching assignments", "School activities", "Configured sports and assigned coaches.", AccessRights.ViewSports, ReportFields.None, A,T),
        new("users", "User access", "Administration", "Account names, roles, groups and active status. Authentication secrets are excluded.", AccessRights.ViewUsers, S, A)
    ];
    public static ReportDefinition? Find(string key) => All.FirstOrDefault(x => x.Key == key);
    public static bool Allowed(ReportDefinition report, WorkspaceAccess access) => report.Workspaces.Contains(access.Workspace) && access.Can(FeatureRights.ViewReports) && access.Can(report.Rights) && access.Can(report.FeatureRight);
    public static Dictionary<int, string> Statuses(string key) => key switch {
        "learners" => Names<EnrolmentStatus>(), "attendance" => Names<AttendanceStatus>(), "prefects" => Names<PrefectStatus>(),
        "nominations" => Names<PrefectAppointmentStatus>(), "trips" => Names<TripStatus>(), "notices" => new() { [1]="Published", [4]="Draft", [2]="Queued", [3]="Delivered" },
        "balances" => new() { [1]="Outstanding", [2]="Settled", [3]="Credit" }, "collections" => new() { [0]="Receipts", [1]="Reversals" },
        "staff" or "sdc" or "users" => new() { [1]="Active", [0]="Inactive" }, "class-teachers" => new() { [1]="Active", [0]="Ended" }, _ => []
    };
    public static string Name<T>(int value) where T : struct, Enum => Enum.IsDefined(typeof(T), value)
        ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(((T)Enum.ToObject(typeof(T), value)).ToString().Replace('_', ' ').ToLowerInvariant()) : $"Unknown ({value})";
    private static Dictionary<int, string> Names<T>() where T : struct, Enum => Enum.GetValues<T>().ToDictionary(x => Convert.ToInt32(x), x => Name<T>(Convert.ToInt32(x)));
}

public static class ReportCsv
{
    public static string Cell(string? value)
    {
        value ??= "";
        var first = value.TrimStart(' ', '\t', '\r', '\n', '\uFEFF');
        if (first.Length > 0 && "=+-@".Contains(first[0]) && !decimal.TryParse(first, NumberStyles.Number, CultureInfo.InvariantCulture, out _)) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public static byte[] Encode(ReportTable report)
    {
        var text = new StringBuilder().AppendLine(string.Join(",", report.Headers.Select(Cell)));
        foreach (var row in report.Rows) text.AppendLine(string.Join(",", row.Select(Cell)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }
}

public sealed partial class SchoolReportService(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access)
{
    private const int MaximumRows = 25000;
    public bool CanRun(string key) => ReportCatalog.Find(key) is { } definition && ReportCatalog.Allowed(definition, access);
    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    private static string Date(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
    private static string DateTimeText(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Person(Student? x) => x == null ? "—" : x.Surname + " " + x.Name;
    private static string StaffName(Staff? x) => x == null ? "—" : x.Surname + " " + x.Name;
    private static string TermName(Term? x) => x == null ? "—" : string.IsNullOrWhiteSpace(x.Name) ? $"{x.AcademicYear} · Term {x.Number}" : x.Name;
    private async Task<List<T>> Load<T>(IQueryable<T> query, CancellationToken ct)
    {
        var rows = await query.Take(MaximumRows + 1).ToListAsync(ct);
        if (rows.Count > MaximumRows) throw new ReportLimitException();
        return rows;
    }
    private static IQueryable<T> Dates<T>(IQueryable<T> query, Expression<Func<T, DateTime?>> date, ReportFilters filters, bool utc = false)
    {
        if (filters.From.HasValue) {
            var start = filters.From.Value.ToDateTime(TimeOnly.MinValue).AddHours(utc ? -2 : 0);
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.GreaterThanOrEqual(date.Body, Expression.Constant((DateTime?)start, typeof(DateTime?))), date.Parameters));
        }
        if (filters.To.HasValue) {
            var end = filters.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(utc ? -2 : 0);
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.LessThan(date.Body, Expression.Constant((DateTime?)end, typeof(DateTime?))), date.Parameters));
        }
        return query;
    }
    public IQueryable<Class> Classes()
    {
        var classes = db.Classes.AsNoTracking();
        if (access.Workspace == SchoolWorkspace.Teacher) {
            var year = DateTime.UtcNow.AddHours(2).Year;
            classes = classes.Where(x => user.UserId.HasValue && db.ClassTeacherAssignments.Any(a => a.ClassId == x.Id && a.AcademicYear == year && a.EndedAt == null && a.Staff.UserId == user.UserId));
        }
        return classes;
    }
    private IQueryable<Student> Students(ReportFilters f)
    {
        var query = db.Students.AsNoTracking();
        if (access.Workspace == SchoolWorkspace.Teacher) { var classes = Classes().Select(x => x.Id); query = query.Where(x => x.ClassId.HasValue && classes.Contains(x.ClassId.Value)); }
        if (f.GradeId.HasValue) query = query.Where(x => x.Class != null && x.Class.GradeId == f.GradeId);
        if (f.ClassId.HasValue) query = query.Where(x => x.ClassId == f.ClassId);
        return query;
    }
    public async Task<ReportTable> GenerateAsync(string key, ReportFilters f, CancellationToken ct = default)
    {
        if (!CanRun(key)) throw new UnauthorizedAccessException("This report is not available to this account.");
        if (f.From > f.To || f.To == DateOnly.MaxValue || f.From?.Year == 1) throw new ArgumentException("Choose a valid date range with the end on or after the start.");
        var statuses = ReportCatalog.Statuses(key);
        if (f.Status.HasValue && !statuses.ContainsKey(f.Status.Value)) throw new ArgumentException("Choose a valid report status.");
        var report = key switch {
            "learners" => await Learners(f, ct), "attendance" => await Attendance(f, ct), "staff" => await StaffReport(f, ct),
            "class-teachers" => await ClassTeachers(f, ct), "subjects" => await Subjects(ct),
            "balances" => await Balances(f, ct), "collections" => await Collections(f, ct), "fee-structures" => await FeeStructures(f, ct),
            "trips" => await Trips(f, ct), "masses" => await Masses(f, ct), "prefects" => await Prefects(f, ct), "nominations" => await Nominations(f, ct),
            "sdc" => await Sdc(f, ct), "notices" => await Notices(f, ct), "houses" => await Houses(ct), "sports" => await Sports(ct), "users" => await Users(f, ct),
            _ => throw new ArgumentException("Unknown report.")
        };
        report.Metrics.Insert(0, new("Records", report.Rows.Count.ToString("N0", CultureInfo.InvariantCulture)));
        if (access.Workspace == SchoolWorkspace.Teacher && key is "learners" or "attendance" or "prefects") report.Notes.Add("Learner data is limited to your current assigned classes. Grade and class labels reflect current placement.");
        return report;
    }
    private async Task<ReportTable> Learners(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(Students(f), x => x.EnrolmentDate, f);
        if (f.Status.HasValue) q = q.Where(x => x.EnrolmentStatusId == f.Status);
        var rows = await Load(q.Include(x => x.Class).ThenInclude(x => x!.Grade).OrderBy(x => x.Surname).ThenBy(x => x.Name).ThenBy(x => x.Id), ct);
        db.ApplyStudentNumbers(rows);
        var result = new ReportTable(["Learner number", "Learner", "Grade", "Class", "Gender", "Enrolment date", "Status"]);
        foreach (var x in rows) result.Add(x.StudentNumber, Person(x), Text(x.Class?.Grade.Name), Text(x.Class?.Name), ReportCatalog.Name<Gender>(x.GenderId), Date(x.EnrolmentDate), ReportCatalog.Name<EnrolmentStatus>(x.EnrolmentStatusId));
        foreach (var group in rows.GroupBy(x => x.Class?.Grade.Name ?? "Unassigned")) result.Metrics.Add(new(group.Key, group.Count().ToString()));
        result.Notes.Add("This is a current learner register. Date filters use recorded enrolment dates; missing enrolment dates are excluded when dates are selected.");
        return result;
    }
    private async Task<ReportTable> Attendance(ReportFilters f, CancellationToken ct)
    {
        var studentIds = Students(f).Select(x => x.Id);
        var q = Dates(db.AttendanceRecords.AsNoTracking().Where(x => studentIds.Contains(x.StudentId)), x => x.Date, f);
        if (f.Status.HasValue) q = q.Where(x => x.Status == f.Status);
        var rows = await Load(q.OrderBy(x => x.Date).ThenBy(x => x.StudentId).ThenBy(x => x.Id), ct);
        var ids = rows.Select(x => x.StudentId).Distinct().ToList();
        var students = await Students(f).Where(x => ids.Contains(x.Id)).Include(x => x.Class).ToDictionaryAsync(x => x.Id, ct);
        var result = new ReportTable(["Date", "Learner", "Class", "Attendance"]);
        foreach (var x in rows) result.Add(Date(x.Date), Person(students.GetValueOrDefault(x.StudentId)), Text(students.GetValueOrDefault(x.StudentId)?.Class?.Name), ReportCatalog.Name<AttendanceStatus>(x.Status));
        foreach (var group in rows.GroupBy(x => x.Status)) result.Metrics.Add(new(ReportCatalog.Name<AttendanceStatus>(group.Key), group.Count().ToString()));
        result.Notes.Add("Only recorded attendance is included. Missing records do not mean absent. Counts describe records, not a percentage of possible school days.");
        return result;
    }
}
