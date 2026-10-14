using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
namespace SMS.Web.Services;
public static class PrefectAccess
{
    public static bool CanManage(WorkspaceAccess access, AccessRights right) => access.Workspace == SchoolWorkspace.Administration && access.Can(right);
    public static bool CanNominate(WorkspaceAccess access) => access.CanNominatePrefects;
    public static IQueryable<Student> EligibleStudents(SMSDbContext db, WorkspaceAccess access, Guid? userId)
    {
        var students = db.Students.AsNoTracking().Where(x => x.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE);
        if (access.Workspace == SchoolWorkspace.Teacher)
        {
            var year = DateTime.UtcNow.AddHours(2).Year;
            students = students.Where(x => x.ClassId.HasValue && db.ClassTeacherAssignments.Any(a => a.ClassId == x.ClassId
                && a.AcademicYear == year && a.EndedAt == null && userId.HasValue && a.Staff.UserId == userId));
        }
        return students;
    }
}
