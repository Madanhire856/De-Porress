using Microsoft.EntityFrameworkCore;
using SMS.Data;

namespace SMS.Web.Pages.GeneratedNumbers;

public static class GeneratedNumberHelper
{
    // Rank the whole table independently of search and pagination. The primary key
    // breaks timestamp ties, including legacy null timestamps.
    private static Dictionary<Guid, int> Ranks(IQueryable<Guid> orderedIds) => orderedIds
        .ToList().Select((id, index) => (id, number: index + 1)).ToDictionary(x => x.id, x => x.number);

    private static void Apply<T>(IQueryable<Guid> orderedIds, IEnumerable<T> items,
        Func<T, Guid> id, Action<T, int> assign)
    {
        var rows = items.ToList();
        if (rows.Count == 0) return;
        var ranks = Ranks(orderedIds);
        foreach (var row in rows) assign(row, ranks.GetValueOrDefault(id(row)));
    }

    private static IQueryable<Guid> UserGroupIds(SMSDbContext db) => db.UserGroups.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetUserGroupSequence(this SMSDbContext db, UserGroup item) => Ranks(UserGroupIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyUserGroupNumber(this SMSDbContext db, UserGroup item) => item.UserGroupSequence = db.GetUserGroupSequence(item);
    public static void ApplyUserGroupNumbers(this SMSDbContext db, IEnumerable<UserGroup> items) =>
        Apply(UserGroupIds(db), items, x => x.Id, (x, number) => x.UserGroupSequence = number);

    private static IQueryable<Guid> UserIds(SMSDbContext db) => db.Users.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetUserSequence(this SMSDbContext db, User item) => Ranks(UserIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyUserNumber(this SMSDbContext db, User item) => item.UserSequence = db.GetUserSequence(item);
    public static void ApplyUserNumbers(this SMSDbContext db, IEnumerable<User> items) =>
        Apply(UserIds(db), items, x => x.Id, (x, number) => x.UserSequence = number);

    private static IQueryable<Guid> SubjectIds(SMSDbContext db) => db.Subjects.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetSubjectSequence(this SMSDbContext db, Subject item) => Ranks(SubjectIds(db)).GetValueOrDefault(item.Id);
    public static void ApplySubjectNumber(this SMSDbContext db, Subject item) => item.SubjectSequence = db.GetSubjectSequence(item);
    public static void ApplySubjectNumbers(this SMSDbContext db, IEnumerable<Subject> items) =>
        Apply(SubjectIds(db), items, x => x.Id, (x, number) => x.SubjectSequence = number);

    private static IQueryable<Guid> ClassIds(SMSDbContext db) => db.Classes.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetClassSequence(this SMSDbContext db, Class item) => Ranks(ClassIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyClassNumber(this SMSDbContext db, Class item) => item.ClassSequence = db.GetClassSequence(item);
    public static void ApplyClassNumbers(this SMSDbContext db, IEnumerable<Class> items) =>
        Apply(ClassIds(db), items, x => x.Id, (x, number) => x.ClassSequence = number);

    private static IQueryable<Guid> GradeIds(SMSDbContext db) => db.Grades.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetGradeSequence(this SMSDbContext db, Grade item) => Ranks(GradeIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyGradeNumber(this SMSDbContext db, Grade item) => item.GradeSequence = db.GetGradeSequence(item);
    public static void ApplyGradeNumbers(this SMSDbContext db, IEnumerable<Grade> items) =>
        Apply(GradeIds(db), items, x => x.Id, (x, number) => x.GradeSequence = number);

    private static IQueryable<Guid> HouseIds(SMSDbContext db) => db.Houses.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetHouseSequence(this SMSDbContext db, House item) => Ranks(HouseIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyHouseNumber(this SMSDbContext db, House item) => item.HouseSequence = db.GetHouseSequence(item);
    public static void ApplyHouseNumbers(this SMSDbContext db, IEnumerable<House> items) =>
        Apply(HouseIds(db), items, x => x.Id, (x, number) => x.HouseSequence = number);

    private static IQueryable<Guid> VillageIds(SMSDbContext db) => db.Villages.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetVillageSequence(this SMSDbContext db, Village item) => Ranks(VillageIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyVillageNumber(this SMSDbContext db, Village item) => item.VillageSequence = db.GetVillageSequence(item);
    public static void ApplyVillageNumbers(this SMSDbContext db, IEnumerable<Village> items) =>
        Apply(VillageIds(db), items, x => x.Id, (x, number) => x.VillageSequence = number);

    private static IQueryable<Guid> TermIds(SMSDbContext db) => db.Terms.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetTermSequence(this SMSDbContext db, Term item) => Ranks(TermIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyTermNumber(this SMSDbContext db, Term item) => item.TermSequence = db.GetTermSequence(item);
    public static void ApplyTermNumbers(this SMSDbContext db, IEnumerable<Term> items) =>
        Apply(TermIds(db), items, x => x.Id, (x, number) => x.TermSequence = number);

    private static IQueryable<Guid> SportIds(SMSDbContext db) => db.Sports.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetSportSequence(this SMSDbContext db, Sport item) => Ranks(SportIds(db)).GetValueOrDefault(item.Id);
    public static void ApplySportNumber(this SMSDbContext db, Sport item) => item.SportSequence = db.GetSportSequence(item);
    public static void ApplySportNumbers(this SMSDbContext db, IEnumerable<Sport> items) =>
        Apply(SportIds(db), items, x => x.Id, (x, number) => x.SportSequence = number);

    private static IQueryable<Guid> StudentIds(SMSDbContext db) => db.Students.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetStudentSequence(this SMSDbContext db, Student item) => Ranks(StudentIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyStudentNumber(this SMSDbContext db, Student item) => item.StudentSequence = db.GetStudentSequence(item);
    public static void ApplyStudentNumbers(this SMSDbContext db, IEnumerable<Student> items) =>
        Apply(StudentIds(db), items, x => x.Id, (x, number) => x.StudentSequence = number);

    private static IQueryable<Guid> FeesStructureIds(SMSDbContext db) => db.FeesStructures.AsNoTracking()
        .OrderBy(x => x.CreationDate).ThenBy(x => x.Id).Select(x => x.Id);
    public static int GetFeesStructureSequence(this SMSDbContext db, FeesStructure item) => Ranks(FeesStructureIds(db)).GetValueOrDefault(item.Id);
    public static void ApplyFeesStructureNumber(this SMSDbContext db, FeesStructure item) => item.FeesStructureSequence = db.GetFeesStructureSequence(item);
    public static void ApplyFeesStructureNumbers(this SMSDbContext db, IEnumerable<FeesStructure> items) =>
        Apply(FeesStructureIds(db), items, x => x.Id, (x, number) => x.FeesStructureSequence = number);
}
