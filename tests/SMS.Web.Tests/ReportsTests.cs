using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using Xunit;
using ReportPage = SMS.Web.Areas.Reports.Pages.RunModel;

namespace SMS.Web.Tests;

public class ReportsTests
{
    private static SMSDbContext Database() => new(new DbContextOptionsBuilder<SMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static SchoolReportService Service(SMSDbContext db, Actor actor) => new(db, actor, new(actor));
    private static ReportPage Page(SMSDbContext db, Actor actor)
    {
        var context = new DefaultHttpContext(); context.Request.Path = "/Reports/Run/learners";
        return new(db, Service(db, actor), new(actor), actor) { PageContext = new PageContext { HttpContext = context } };
    }
    public static IEnumerable<object[]> Keys => ReportCatalog.All.Select(x => new object[] { x.Key });

    [Theory, MemberData(nameof(Keys))]
    public async Task EveryAvailableReportHandlesAnEmptyDatabase(string key)
    {
        using var db = Database(); var result = await Service(db, new()).GenerateAsync(key, new());
        Assert.NotEmpty(result.Headers); Assert.Empty(result.Rows); Assert.Equal("0", result.Metrics[0].Value);
    }
    [Fact]
    public async Task TeacherLearnerAndAttendanceReportsCannotEscapeAssignedClasses()
    {
        using var db = Database(); var actor = new Actor(UserRole.STAFF);
        var own = new Class { Id = Guid.NewGuid(), Name = "Own class", Grade = new Grade { Id = Guid.NewGuid(), Name = "Grade 5" } };
        var other = new Class { Id = Guid.NewGuid(), Name = "Other class", Grade = new Grade { Id = Guid.NewGuid(), Name = "Grade 6" } };
        var staff = new Staff { Id = Guid.NewGuid(), UserId = actor.UserId, Name = "Teacher", Surname = "Own", IdNumber = "TEST" };
        db.Classes.AddRange(own, other); db.Staff.Add(staff);
        db.ClassTeacherAssignments.Add(new() { Id = Guid.NewGuid(), ClassId = own.Id, StaffId = staff.Id, AcademicYear = DateTime.UtcNow.AddHours(2).Year });
        var mine = new Student { Id = Guid.NewGuid(), Name = "Visible", Surname = "Student", ClassId = own.Id };
        var hidden = new Student { Id = Guid.NewGuid(), Name = "Hidden", Surname = "Student", ClassId = other.Id };
        db.Students.AddRange(mine, hidden);
        db.AttendanceRecords.AddRange(new AttendanceRecord { Id = Guid.NewGuid(), StudentId = mine.Id, Date = new DateTime(2026, 10, 18) }, new AttendanceRecord { Id = Guid.NewGuid(), StudentId = hidden.Id, Date = new DateTime(2026, 10, 18) });
        await db.SaveChangesAsync(); var service = Service(db, actor);
        var register = await service.GenerateAsync("learners", new()); Assert.Single(register.Rows); Assert.Contains("Student Visible", register.Rows[0]);
        Assert.Empty((await service.GenerateAsync("learners", new() { ClassId = other.Id })).Rows);
        Assert.Single((await service.GenerateAsync("attendance", new())).Rows);
        Assert.Empty((await service.GenerateAsync("attendance", new() { GradeId = other.GradeId })).Rows);
        Assert.DoesNotContain("Hidden", Encoding.UTF8.GetString(ReportCsv.Encode(register)));
    }
    [Fact]
    public async Task AttendanceUsesInclusiveDatesWithoutInventingMissingDays()
    {
        using var db = Database(); var student = new Student { Id = Guid.NewGuid(), Name = "One", Surname = "Student" }; db.Students.Add(student);
        db.AttendanceRecords.AddRange(new AttendanceRecord { Id = Guid.NewGuid(), StudentId = student.Id, Date = new DateTime(2026, 10, 18, 23, 59, 0), Status = 0 }, new AttendanceRecord { Id = Guid.NewGuid(), StudentId = student.Id, Date = new DateTime(2026, 10, 19), Status = 1 });
        await db.SaveChangesAsync();
        var data = await Service(db, new()).GenerateAsync("attendance", new() { From = new(2026, 10, 18), To = new(2026, 10, 18) });
        Assert.Single(data.Rows); Assert.Equal("Present", data.Rows[0][3]);
        Assert.DoesNotContain(data.Metrics, x => x.Label.Contains("rate", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task CollectionsKeepCurrenciesSeparateSubtractReversalsAndFlagMissingFx()
    {
        using var db = Database(); var actor = new Actor();
        db.Currencies.Add(new() { Code = "USD", Name = "US Dollar", Symbol = "$", IsBase = true });
        void Payment(decimal amount, string currency, decimal? baseAmount, bool reversal = false, int day = 18) => db.Payments.Add(new() {
            Id = Guid.NewGuid(), CreatorId = actor.UserId!.Value, CreatedByName = "Recorder", Amount = amount, CurrencyId = currency, BaseAmount = baseAmount, IsReversal = reversal,
            PaymentDate = new DateTime(2026, 10, day), CreationDate = new DateTime(2026, 10, day), ReceiptYear = 2026
        });
        Payment(100, "USD", null); Payment(-20, "USD", -20, true); Payment(40, "EUR", 50); Payment(30, "EUR", null); Payment(999, "USD", 999, false, 19);
        await db.SaveChangesAsync();
        var data = await Service(db, actor).GenerateAsync("collections", new() { From = new(2026, 10, 18), To = new(2026, 10, 18) });
        Assert.Equal(4, data.Rows.Count);
        Assert.Contains(new ReportMetric("Net · USD", "80.00"), data.Metrics); Assert.Contains(new ReportMetric("Net · EUR", "70.00"), data.Metrics);
        Assert.Contains(new ReportMetric("Recorded base net · USD", "130.00"), data.Metrics);
        Assert.Contains(data.Notes, x => x.StartsWith("1 record(s) lack"));
        Assert.Contains(data.Rows, x => x[8] == "-20.00" && x[10] == "-20.00");
    }
    [Fact]
    public async Task CollectionsWithoutSchoolWideRightStayScopedToRecorderEvenForAdministrators()
    {
        using var db = Database(); var actor = new Actor(UserRole.ADMIN, AccessRights.ViewFeeReports | AccessRights.ViewFeeBalances);
        db.Payments.AddRange(new Payment { Id = Guid.NewGuid(), CreatorId = actor.UserId!.Value, CreatedByName = "Mine", Amount = 5, PaymentDate = DateTime.Today }, new Payment { Id = Guid.NewGuid(), CreatorId = Guid.NewGuid(), CreatedByName = "Other recorder", Amount = 1000, PaymentDate = DateTime.Today });
        await db.SaveChangesAsync(); var result = await Service(db, actor).GenerateAsync("collections", new());
        Assert.Single(result.Rows); Assert.Equal("Mine", result.Rows[0][4]); Assert.DoesNotContain("Other recorder", Encoding.UTF8.GetString(ReportCsv.Encode(result)));
    }
    [Fact]
    public async Task BalancesNeverNetOffCreditsAgainstDebtOrMixCurrencies()
    {
        using var db = Database(); var student = new Student { Id = Guid.NewGuid(), Name = "One", Surname = "Student" };
        var term = new Term { Id = Guid.NewGuid(), Name = "Term 1" }; db.AddRange(student, term);
        foreach (var (currency, amount) in new[] { ("USD", 100m), ("USD", -25m), ("EUR", 10m) }) db.StudentLedgers.Add(new() { Id = Guid.NewGuid(), StudentId = student.Id, TermId = term.Id, CurrencyId = currency, ClosingBalance = amount });
        await db.SaveChangesAsync(); var result = await Service(db, new()).GenerateAsync("balances", new());
        Assert.Contains(new ReportMetric("Outstanding · USD", "100.00"), result.Metrics); Assert.Contains(new ReportMetric("Credit · USD", "25.00"), result.Metrics);
        Assert.Contains(new ReportMetric("Outstanding · EUR", "10.00"), result.Metrics);
        Assert.Single((await Service(db, new()).GenerateAsync("balances", new() { Status = 3 })).Rows);
    }
    [Fact]
    public async Task AllReportHandlersRecheckPermissionsAndRejectUnknownReports()
    {
        using var db = Database(); var actor = new Actor(UserRole.STAFF, AccessRights.ViewLearners); var page = Page(db, actor);
        Assert.IsType<ForbidResult>(await page.OnGetAsync("collections", default));
        Assert.IsType<ForbidResult>(await page.OnGetExportAsync("collections", default));
        Assert.IsType<ForbidResult>(await page.OnGetPrintAsync("collections", default));
        Assert.IsType<NotFoundResult>(await page.OnGetAsync("exam-results", default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(db, actor).GenerateAsync("users", new()));
    }
    [Fact]
    public async Task CsvAndPrintIncludeAllFilteredRowsRatherThanOnlyCurrentPage()
    {
        using var db = Database(); var actor = new Actor();
        for (var i = 0; i < 28; i++) db.Students.Add(new() { Id = Guid.NewGuid(), Name = "Learner" + i.ToString("D2"), Surname = "Example" });
        await db.SaveChangesAsync(); var page = Page(db, actor); page.PageSize = 1; page.PageNumber = 2;
        Assert.IsType<PageResult>(await page.OnGetAsync("learners", default)); Assert.Single(page.Rows); Assert.Equal(28, page.Data.Rows.Count);
        var export = Assert.IsType<FileContentResult>(await page.OnGetExportAsync("learners", default));
        var csv = Encoding.UTF8.GetString(export.FileContents); Assert.Contains("Learner00", csv); Assert.Contains("Learner27", csv);
        Assert.IsType<PageResult>(await page.OnGetPrintAsync("learners", default)); Assert.Equal(28, page.Rows.Count);
        Assert.Equal("no-store", page.Response.Headers.CacheControl.ToString());
    }
    [Fact]
    public void CsvNeutralizesFormulasAndPreservesQuotesNewlinesAndNumericReversals()
    {
        Assert.Equal("\"'=HYPERLINK(\"\"https://example.test\"\")\"", ReportCsv.Cell("=HYPERLINK(\"https://example.test\")"));
        Assert.StartsWith("\"'", ReportCsv.Cell("\t+cmd|' /C calc'!A0"));
        Assert.Equal("\"-20.00\"", ReportCsv.Cell("-20.00"));
        Assert.Equal("\"a,\"\"b\"\"\nc\"", ReportCsv.Cell("a,\"b\"\nc"));
    }
    [Fact]
    public async Task UserAccessExportContainsNoAuthenticationSecrets()
    {
        using var db = Database(); db.Users.Add(new() { Id = Guid.NewGuid(), LoginId = "User", Name = "Example", Email = "example@test.invalid", PasswordHash = "PASSWORD-SECRET", AuthRecoveryCodes = "RECOVERY-SECRET", AuthenticatorKey = "AUTHENTICATOR-SECRET" }); await db.SaveChangesAsync();
        var csv = Encoding.UTF8.GetString(ReportCsv.Encode(await Service(db, new()).GenerateAsync("users", new())));
        Assert.Contains("example@test.invalid", csv); Assert.DoesNotContain("SECRET", csv);
    }
    [Fact]
    public async Task InvalidRangesAndStatusesProduceErrorsRatherThanMisleadingExports()
    {
        using var db = Database(); var actor = new Actor(); var page = Page(db, actor);
        page.Filters = new() { From = new(2026, 10, 19), To = new(2026, 10, 18) };
        Assert.IsType<BadRequestObjectResult>(await page.OnGetExportAsync("collections", default));
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db, actor).GenerateAsync("learners", new() { Status = 999 }));
    }
    [Fact]
    public void ExportLinksKeepDatesAndRepeatedInvariantHintsWithoutCarryingOldPagination()
    {
        using var db = Database(); var page = Page(db, new());
        page.Request.QueryString = new QueryString("?Filters.From=2026-10-01&__Invariant=Filters.From&Filters.To=2026-10-18&__Invariant=Filters.To&PageNumber=4&Filters.Status=1");
        page.Filters = new() { From = new(2026, 10, 1), To = new(2026, 10, 18), Status = 1 };
        var link = page.Link("Export");
        var values = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(link[link.IndexOf('?')..]);
        Assert.Equal(2, values["__Invariant"].Count); Assert.Equal("Export", values["handler"].ToString());
        Assert.False(values.ContainsKey("PageNumber")); Assert.Equal("1", values["Filters.Status"].ToString());
    }
    public sealed class Actor(UserRole role = UserRole.ADMIN, AccessRights rights = (AccessRights)(-1L)) : ICurrentUserService
    {
        public string? Email => "reporter@example.test";
        public string? Name => "Reporter";
        public string? IdentitySubject => "test";
        public bool HasFeatureRight(FeatureRights right) => IsAuthenticated && ((FeatureRightsCatalog.Legacy(Rights) & (long)right) == (long)right);
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public int? RoleId => (int)role;
        public Guid? GroupId => null;
        public long Rights => (long)rights;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights right) => (rights & right) == right;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(UserId!.Value);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult(UserId);
    }
}
