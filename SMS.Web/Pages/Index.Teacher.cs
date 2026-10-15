using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;

namespace SMS.Web.Pages;

public partial class IndexModel
{
    public bool TeacherStaffLinked { get; private set; }
    public int TeacherPaidUpStudents { get; private set; }
    public int TeacherStudentsOwing { get; private set; }
    public int TeacherStudentsWithoutAccount { get; private set; }
    public int TeacherAttendanceRecorded { get; private set; }
    public List<TeacherBalanceSummary> TeacherBalances { get; private set; } = [];
    public List<TeacherAssignmentSummary> TeacherHouses { get; private set; } = [];
    public List<TeacherAssignmentSummary> TeacherSports { get; private set; } = [];
    public List<string> TeacherSubjects { get; private set; } = [];
    public List<TeacherMassSummary> TeacherMasses { get; private set; } = [];
    public int UpcomingMassCount { get; private set; }
    public int AssignedMassCount { get; private set; }
    public record TeacherBalanceSummary(string Currency, decimal Amount);
    public record TeacherAssignmentSummary(Guid Id, string Name, bool Assigned);
    public record TeacherMassSummary(DateTime Date, string Venue, string Celebrant, bool Assigned, int ClassParticipants);

    // The teacher role gets summary data by default. Record-level pages still enforce their own rights.
    // Every student and fee query is restricted to classes linked to this authenticated user's staff record.
    private async Task LoadTeacherDashboardAsync(SMSDbContext db, Guid? userId, CancellationToken cancellationToken)
    {
        var hasUser = userId.HasValue;
        TeacherStaffLinked = hasUser && await db.Staff.AsNoTracking().AnyAsync(staff => staff.UserId == userId, cancellationToken);
        CurrentTerm = await db.Terms.AsNoTracking().Where(term => term.StartDate <= SchoolToday && term.EndDate >= SchoolToday)
            .OrderByDescending(term => term.StartDate).FirstOrDefaultAsync(cancellationToken);
        var assignedClassIds = db.ClassTeacherAssignments.Where(assignment => hasUser
            && assignment.AcademicYear == SchoolToday.Year && assignment.EndedAt == null
            && assignment.Staff.IsActive == true && assignment.Staff.UserId == userId).Select(assignment => assignment.ClassId);
        TeacherClasses = await db.Classes.AsNoTracking()
            .Where(classroom => assignedClassIds.Contains(classroom.Id))
            .OrderBy(classroom => classroom.Name)
            .Select(classroom => new TeacherClassSummary(classroom.Id, classroom.Name, classroom.Grade.Name,
                classroom.Students.Count(student => student.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE)))
            .ToListAsync(cancellationToken);
        var students = db.Students.AsNoTracking().Where(student => hasUser
            && student.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE
            && student.Class != null && assignedClassIds.Contains(student.Class.Id));
        var studentIds = students.Select(student => student.Id);
        TeacherActiveStudents = await students.CountAsync(cancellationToken);
        var tomorrow = SchoolToday.AddDays(1);
        TeacherAttendanceRecorded = await db.AttendanceRecords.AsNoTracking()
            .Where(record => studentIds.Contains(record.StudentId) && record.Date >= SchoolToday && record.Date < tomorrow)
            .Select(record => record.StudentId).Distinct().CountAsync(cancellationToken);
        if (CurrentTerm != null)
        {
            var termId = CurrentTerm.Id;
            var ledgers = db.StudentLedgers.AsNoTracking().Where(ledger => ledger.TermId == termId && studentIds.Contains(ledger.StudentId));
            TeacherStudentsOwing = await ledgers.Where(ledger => ledger.ClosingBalance > 0)
                .Select(ledger => ledger.StudentId).Distinct().CountAsync(cancellationToken);
            TeacherStudentsWithoutAccount = await students.CountAsync(student => !student.StudentLedgers.Any(ledger => ledger.TermId == termId), cancellationToken);
            TeacherPaidUpStudents = TeacherActiveStudents - TeacherStudentsOwing - TeacherStudentsWithoutAccount;
            TeacherBalances = await ledgers.Where(ledger => ledger.ClosingBalance > 0)
                .GroupBy(ledger => ledger.CurrencyId)
                .OrderBy(group => group.Key)
                .Select(group => new TeacherBalanceSummary(group.Key ?? "Unspecified currency", group.Sum(ledger => ledger.ClosingBalance)))
                .ToListAsync(cancellationToken);
        }
        TeacherHouses = await db.Houses.AsNoTracking().OrderBy(house => house.Name)
            .Select(house => new TeacherAssignmentSummary(house.Id, house.Name,
                house.HouseTeachers.Any(link => hasUser && link.Staff.UserId == userId))).ToListAsync(cancellationToken);
        TeacherSports = await db.Sports.AsNoTracking().OrderBy(sport => sport.Name)
            .Select(sport => new TeacherAssignmentSummary(sport.Id, sport.Name,
                sport.SportTeachers.Any(link => hasUser && link.Staff.UserId == userId))).ToListAsync(cancellationToken);
        TeacherSubjects = await db.TeacherSubjects.AsNoTracking().Where(link => hasUser && link.Staff.UserId == userId)
            .Select(link => link.Subject.Name).Distinct().OrderBy(name => name).ToListAsync(cancellationToken);
        var masses = db.MassEvents.AsNoTracking().Where(mass => mass.DateTime >= SchoolToday);
        UpcomingMassCount = await masses.CountAsync(cancellationToken);
        AssignedMassCount = await masses.CountAsync(mass => mass.MassRosters.Any(roster =>
            (hasUser && roster.Staff != null && roster.Staff.UserId == userId)
            || (roster.StudentId.HasValue && studentIds.Contains(roster.StudentId.Value))), cancellationToken);
        TeacherMasses = await masses.OrderBy(mass => mass.DateTime).ThenBy(mass => mass.Id).Take(5)
            .Select(mass => new TeacherMassSummary(mass.DateTime, mass.Venue, mass.Celebrant,
                mass.MassRosters.Any(roster => hasUser && roster.Staff != null && roster.Staff.UserId == userId),
                mass.MassRosters.Where(roster => roster.StudentId.HasValue && studentIds.Contains(roster.StudentId.Value))
                    .Select(roster => roster.StudentId).Distinct().Count())).ToListAsync(cancellationToken);
    }
}
