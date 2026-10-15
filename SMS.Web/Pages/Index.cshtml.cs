using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
namespace SMS.Web.Pages;
public partial class IndexModel(SMSDbContext context, ICurrentUserService currentUser, WorkspaceAccess access) : PageModel
{
    public bool IsAdmin { get; private set; }
    public bool IsBursar { get; private set; }
    public bool IsTeacher => access.Workspace == SchoolWorkspace.Teacher;

    public bool CanOpen(string area, string page) => access.CanOpen(area, page);
    public bool Can(AccessRights right) => access.Can(right);
    public IEnumerable<WorkspaceFeature> WorkspaceLinks => access.Navigation;
    public List<TeacherClassSummary> TeacherClasses { get; private set; } = [];
    public int TeacherActiveStudents { get; private set; }
    public string? TeacherAccessGroup { get; private set; }
    public int TeacherGrantedRights { get; private set; }
    public bool CanViewFinance => (IsAdmin || IsBursar) && currentUser.HasRight(AccessRights.ViewFeeBalances);
    public bool CanRecordPayments => currentUser.HasRight(AccessRights.RecordPayments);
    public bool CanViewAllCollections => IsAdmin || currentUser.HasRight(AccessRights.ViewAllBursarsCashUp);
    public string BaseCurrency { get; private set; } = "USD";
    public DateTime ReportingStart => SchoolToday.AddDays(-6);
    public List<DailyCollection> DailyCollections { get; private set; } = [];
    public decimal MonthCollected => DailyCollections.Sum(d => d.Collected);
    public decimal MonthReversed => DailyCollections.Sum(d => d.Reversed);
    public decimal MonthNet => MonthCollected - MonthReversed;
    public decimal WeekNet { get; private set; }
    public int MissingBaseAmounts { get; private set; }
    public List<EnrolmentSummary> RecentEnrolments { get; private set; } = [];
    public int RecentEnrolmentCount { get; private set; }
    public decimal TodayCollected { get; private set; }
    public decimal TodayReversed { get; private set; }
    public decimal Outstanding { get; private set; }
    public int LedgerCount { get; private set; }
    public int SettledLedgers { get; private set; }
    public int OverdueLedgers { get; private set; }
    public int PartialLedgers { get; private set; }
    public int TodayReceipts { get; private set; }
    public List<Payment> RecentPayments { get; private set; } = [];
    public const int RecentReceiptsLimit = 5;
    public int RecentReceiptCount { get; private set; }
    public List<StudentLedger> TopBalances { get; private set; } = [];
    public const int LargestBalancesLimit = 5;
    public int OutstandingAccounts { get; private set; }
    public DateTime TrendStart => new(SchoolToday.Year, SchoolToday.Month, 1);
    public List<DailyCollection> CollectionTrend { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public DateTime? TrendFrom { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? TrendTo { get; set; }
    public List<WeeklyCollectionPeriods.Period> TrendWeeks { get; private set; } = [];
    public decimal[] TrendReversals => TrendWeeks.Select(week => CollectionTrend
        .Where(day => day.Date >= week.Start && day.Date <= week.End).Sum(day => day.Reversed)).ToArray();
    public List<MethodTrend> MethodTrends { get; private set; } = [];
    public record MethodTrend(int Id, string Name, decimal[] Amounts);
    [BindProperty(SupportsGet = true)] public DateTime? CollectionDate { get; set; }
    public List<MethodCollection> MethodCollections { get; private set; } = [];
    public decimal SelectedCollected => MethodCollections.Sum(m => m.Amount);
    public decimal SelectedReversed { get; private set; }
    public record MethodCollection(int Id, string Name, decimal Amount);
    public string DisplayName => currentUser.Name ?? "Administrator";
    public DateTime SchoolToday { get; } = DateTime.UtcNow.AddHours(2).Date;
    public int Students { get; private set; }
    public int ActiveStaff { get; private set; }
    public int Classes { get; private set; }
    public int ActiveUsers { get; private set; }
    public int InactiveUsers { get; private set; }
    public int UnassignedStudents { get; private set; }
    public int ClassesWithoutTeachers { get; private set; }
    public Term? CurrentTerm { get; private set; }
    public List<GradeSummary> Grades { get; private set; } = [];
    public List<Announcement> PublicNotices { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public bool Website { get; set; }
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (Website || User.Identity?.IsAuthenticated != true)
        {
            PublicNotices = await SchoolNotices.Published(context).OrderByDescending(x => x.CreationDate)
                .ThenByDescending(x => x.Id).Take(3).ToListAsync(cancellationToken);
            return;
        }
        IsAdmin = access.Workspace == SchoolWorkspace.Administration;
        IsBursar = access.Workspace == SchoolWorkspace.Bursar;

        if (IsTeacher)
        {
            TeacherGrantedRights = Enum.GetValues<AccessRights>().Count(right => right != AccessRights.None && currentUser.HasRight(right))
                + (currentUser.CanNominatePrefects ? 1 : 0);
            if (currentUser.GroupId.HasValue)
                TeacherAccessGroup = await context.UserGroups.AsNoTracking()
                    .Where(group => group.Id == currentUser.GroupId.Value)
                    .Select(group => group.Name).FirstOrDefaultAsync(cancellationToken);
            await LoadTeacherDashboardAsync(context, currentUser.UserId, cancellationToken);
            if (Can(AccessRights.ViewLearners) && currentUser.UserId.HasValue)
            {
                var userId = currentUser.UserId.Value;
                await LoadEnrolmentsAsync(context.Students.AsNoTracking().Where(student => student.Class != null
                    && student.Class.ClassTeacher != null && student.Class.ClassTeacher.UserId == userId), cancellationToken);
            }
            return;
        }
        if (!IsAdmin && IsBursar)
        {
            await LoadBursarAsync(cancellationToken);
            return;
        }
        if (!IsAdmin) return;
        var students = context.Students.AsNoTracking().Where(s => s.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE);
        if (Can(AccessRights.ViewLearners))
        {
            Students = await students.CountAsync(cancellationToken);
            UnassignedStudents = await students.CountAsync(s => s.ClassId == null, cancellationToken);
        }
        if (Can(AccessRights.ViewStaff)) ActiveStaff = await context.Staff.CountAsync(s => s.IsActive == true, cancellationToken);
        if (Can(AccessRights.ViewClasses))
        {
            Classes = await context.Classes.CountAsync(cancellationToken);
            ClassesWithoutTeachers = await context.Classes.CountAsync(c => c.ClassTeacherId == null, cancellationToken);
        }
        if (Can(AccessRights.ViewUsers))
        {
            ActiveUsers = await context.Users.CountAsync(u => u.IsActive, cancellationToken);
            InactiveUsers = await context.Users.CountAsync(u => !u.IsActive, cancellationToken);
        }
        if (Can(AccessRights.ViewTerms)) CurrentTerm = await context.Terms.AsNoTracking().Where(t => t.StartDate <= SchoolToday && t.EndDate >= SchoolToday).OrderByDescending(t => t.StartDate).FirstOrDefaultAsync(cancellationToken);
        if (Can(AccessRights.ViewGrades) && Can(AccessRights.ViewLearners)) Grades = await context.Grades.AsNoTracking().OrderBy(g => g.GradeLevelId).ThenBy(g => g.Name).Select(g => new GradeSummary(g.Name, g.Classes.SelectMany(c => c.Students).Count(s => s.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE))).ToListAsync(cancellationToken);
        if (Can(AccessRights.ViewLearners)) await LoadEnrolmentsAsync(context.Students.AsNoTracking(), cancellationToken);
        await LoadBursarAsync(cancellationToken);
    }
    public record GradeSummary(string Name, int Count);
    public record TeacherClassSummary(Guid Id, string Name, string Grade, int? Students);
    public record DailyCollection(DateTime Date, int Receipts, decimal Collected, decimal Reversed);
    public record EnrolmentSummary(Guid Id, string Name, string Class, DateTime? Date);

    private async Task LoadEnrolmentsAsync(IQueryable<Student> students, CancellationToken cancellationToken)
    {
        var nextDay = SchoolToday.AddDays(1);
        var recent = students.Where(s => s.EnrolmentDate >= ReportingStart && s.EnrolmentDate < nextDay);
        RecentEnrolmentCount = await recent.CountAsync(cancellationToken);
        RecentEnrolments = await recent.OrderByDescending(s => s.EnrolmentDate).ThenBy(s => s.Surname).ThenBy(s => s.Id)
            .Take(10).Select(s => new EnrolmentSummary(s.Id, s.Name + " " + s.Surname,
                s.Class == null ? "Not assigned" : s.Class.Name, s.EnrolmentDate)).ToListAsync(cancellationToken);
    }

    private async Task LoadBursarAsync(CancellationToken cancellationToken)
    {
        BaseCurrency = await context.Currencies.AsNoTracking().Where(c => c.IsBase)
            .Select(c => c.Code).FirstOrDefaultAsync(cancellationToken) ?? "USD";
        CurrentTerm = await context.Terms.AsNoTracking().Where(t => t.StartDate <= SchoolToday && t.EndDate >= SchoolToday)
            .OrderByDescending(t => t.StartDate).FirstOrDefaultAsync(cancellationToken);
        if (CurrentTerm != null)
        {
            var ledgers = context.StudentLedgers.AsNoTracking().Where(l => l.TermId == CurrentTerm.Id);
            LedgerCount = await ledgers.CountAsync(cancellationToken);
            SettledLedgers = await ledgers.CountAsync(l => l.StatusId == (int)StudentLedgerStatus.SETTLED, cancellationToken);
            OverdueLedgers = await ledgers.CountAsync(l => l.StatusId == (int)StudentLedgerStatus.OVER_DUE, cancellationToken);
            PartialLedgers = await ledgers.CountAsync(l => l.StatusId == (int)StudentLedgerStatus.PARTIALLY_SETTLED, cancellationToken);
            Outstanding = await ledgers.Where(l => l.ClosingBalance > 0).SumAsync(l => (decimal?)l.ClosingBalance, cancellationToken) ?? 0m;
            var outstanding = ledgers.Where(l => l.ClosingBalance > 0);
            OutstandingAccounts = await outstanding.CountAsync(cancellationToken);
            TopBalances = await outstanding.Include(l => l.Student)
                .OrderByDescending(l => l.ClosingBalance).ThenBy(l => l.Id)
                .Take(LargestBalancesLimit).ToListAsync(cancellationToken);
        }
        var payments = context.Payments.AsNoTracking().AsQueryable();
        if (!CanViewAllCollections)
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            payments = payments.Where(p => p.CreatorId == userId);
        }
        var nextDay = SchoolToday.AddDays(1);
        var today = payments.Where(p => p.PaymentDate >= SchoolToday && p.PaymentDate < nextDay);
        TodayReceipts = await today.CountAsync(p => !p.IsReversal, cancellationToken);
        TodayCollected = await today.Where(p => !p.IsReversal).SumAsync(p => p.BaseAmount ?? p.Amount ?? 0m, cancellationToken);
        TodayReversed = Math.Abs(await today.Where(p => p.IsReversal).SumAsync(p => p.BaseAmount ?? p.Amount ?? 0m, cancellationToken));
        var recent = payments.Where(p => p.PaymentDate >= ReportingStart && p.PaymentDate < nextDay);
        MissingBaseAmounts = await payments.Where(p => p.PaymentDate >= TrendStart && p.PaymentDate < nextDay).CountAsync(p => p.BaseAmount == null && p.CurrencyId != BaseCurrency, cancellationToken);
        var trend = payments.Where(p => p.PaymentDate >= TrendStart && p.PaymentDate < nextDay);
        CollectionDate = CollectionDate?.Date ?? SchoolToday;
        if (CollectionDate < TrendStart || CollectionDate > SchoolToday) CollectionDate = SchoolToday;
        var selectedDate = CollectionDate.Value;
        var methodAmounts = await trend.Where(p => !p.IsReversal && p.PaymentDate >= selectedDate && p.PaymentDate < selectedDate.AddDays(1))
            .GroupBy(p => p.PaymentMethodId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(p => p.BaseAmount ?? (p.CurrencyId == BaseCurrency ? p.Amount ?? 0m : 0m)) })
            .ToListAsync(cancellationToken);
        MethodCollections = Enum.GetValues<PaymentMethod>().Select(method => new MethodCollection((int)method,
            method.ToDisplayName(), methodAmounts.Where(m => m.Id == (int)method).Sum(m => m.Amount))).ToList();
        var daily = await trend.GroupBy(p => p.PaymentDate.Date).Select(g => new DailyCollection(g.Key,
            g.Count(p => !p.IsReversal),
            g.Where(p => !p.IsReversal).Sum(p => p.BaseAmount ?? (p.CurrencyId == BaseCurrency ? p.Amount ?? 0m : 0m)),
            g.Where(p => p.IsReversal).Sum(p => p.BaseAmount ?? (p.CurrencyId == BaseCurrency ? p.Amount ?? 0m : 0m))))
            .ToListAsync(cancellationToken);
        CollectionTrend = daily.OrderBy(d => d.Date).Select(d => d with { Reversed = Math.Abs(d.Reversed) }).ToList();
        TrendFrom = TrendFrom?.Date ?? TrendStart;
        TrendTo = TrendTo?.Date ?? SchoolToday;
        if (TrendFrom < TrendStart || TrendFrom > SchoolToday) TrendFrom = TrendStart;
        if (TrendTo < TrendFrom || TrendTo > SchoolToday) TrendTo = SchoolToday;
        TrendWeeks = WeeklyCollectionPeriods.Create(TrendFrom.Value, TrendTo.Value);
        var trendEnd = TrendTo.Value.AddDays(1);
        var methodDaily = await trend.Where(p => !p.IsReversal && p.PaymentDate >= TrendFrom.Value && p.PaymentDate < trendEnd)
            .GroupBy(p => new { Date = p.PaymentDate.Date, p.PaymentMethodId })
            .Select(g => new { g.Key.Date, Id = g.Key.PaymentMethodId,
                Amount = g.Sum(p => p.BaseAmount ?? (p.CurrencyId == BaseCurrency ? p.Amount ?? 0m : 0m)) })
            .ToListAsync(cancellationToken);
        var methodIds = Enum.GetValues<PaymentMethod>().Select(method => (int)method)
            .Union(methodDaily.Select(row => row.Id)).OrderBy(id => id);
        MethodTrends = methodIds.Select(id => new MethodTrend(id,
            Enum.IsDefined(typeof(PaymentMethod), id) ? ((PaymentMethod)id).ToDisplayName() : $"Other method ({id})",
            TrendWeeks.Select(week => methodDaily.Where(row => row.Id == id && row.Date >= week.Start && row.Date <= week.End)
                .Sum(row => row.Amount)).ToArray())).ToList();
        DailyCollections = CollectionTrend;
        SelectedReversed = DailyCollections.FirstOrDefault(d => d.Date == selectedDate)?.Reversed ?? 0m;
        WeekNet = await recent.SumAsync(p => (decimal?)(p.BaseAmount ?? (p.CurrencyId == BaseCurrency ? p.Amount ?? 0m : 0m)), cancellationToken) ?? 0m;
        RecentReceiptCount = await recent.CountAsync(cancellationToken);
        RecentPayments = await recent.Include(p => p.Ledger).ThenInclude(l => l!.Student)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreationDate).ThenByDescending(p => p.Id)
            .Take(RecentReceiptsLimit).ToListAsync(cancellationToken);
    }
}
