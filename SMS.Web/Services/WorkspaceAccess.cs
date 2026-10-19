using SMS.Lib;

namespace SMS.Web.Services;

public enum SchoolWorkspace { Unassigned, Administration, Bursar, Teacher }

public record WorkspaceFeature(string Title, string Area, string Page, string Group,
    string Icon, AccessRights Right, params SchoolWorkspace[] Workspaces)
{
    public bool RequiresNominationPermission { get; init; }
    public FeatureRights FeatureRight { get; init; }
}

// One catalogue for navigation, dashboard links, and page relevance.
public class WorkspaceAccess(ICurrentUserService user)
{
    public SchoolWorkspace Workspace => Resolve(user.RoleId ?? 0);
    public bool CanViewDashboard => user.IsAuthenticated && Workspace != SchoolWorkspace.Unassigned;
    public string Name => Workspace switch
    {
        SchoolWorkspace.Administration => "Administration",
        SchoolWorkspace.Bursar => "Bursar",
        SchoolWorkspace.Teacher => "Teacher",
        _ => "Staff"
    };

    public static SchoolWorkspace Resolve(int roleId)
    {
        var roles = (UserRole)roleId;
        if ((roles & (UserRole.ADMIN | UserRole.HEAD | UserRole.DEPUTY_HEAD)) != 0) return SchoolWorkspace.Administration;
        if ((roles & UserRole.BURSER) != 0) return SchoolWorkspace.Bursar;
        if ((roles & (UserRole.CLASS_TEACHER | UserRole.STAFF)) != 0) return SchoolWorkspace.Teacher;
        return SchoolWorkspace.Unassigned;
    }

    public static IReadOnlyList<WorkspaceFeature> Features { get; } =
    [
        new("Reports", "Reports", "/Index", "Reports", "fa-chart-column", AccessRights.None, SchoolWorkspace.Administration, SchoolWorkspace.Teacher, SchoolWorkspace.Bursar) { FeatureRight = FeatureRights.ViewReports },
        new("Members", "Academics", "/Sdc/Index", "SDC", "fa-users", AccessRights.None, SchoolWorkspace.Administration) { FeatureRight = FeatureRights.ViewSdcMembers },
        new("Positions", "Academics", "/Sdc/Positions", "SDC", "fa-user-tie", AccessRights.None, SchoolWorkspace.Administration) { FeatureRight = FeatureRights.ViewSdcPositions },
        new("Committee terms", "Academics", "/Sdc/Terms", "SDC", "fa-calendar-alt", AccessRights.None, SchoolWorkspace.Administration) { FeatureRight = FeatureRights.ViewSdcTerms },
        new("School notices", "Academics", "/Notices/Index", "Notices", "fa-bullhorn", AccessRights.None, SchoolWorkspace.Administration) { FeatureRight = FeatureRights.ViewNotices },
        new("Trips", "Academics", "/Trips/Index", "Academics", "fa-bus", AccessRights.None, SchoolWorkspace.Administration, SchoolWorkspace.Teacher, SchoolWorkspace.Bursar) { FeatureRight = FeatureRights.ViewTrips },
        new("Staff notices", "Academics", "/StaffNotices/Index", "Notices", "fa-bullhorn", AccessRights.None, SchoolWorkspace.Administration, SchoolWorkspace.Teacher, SchoolWorkspace.Bursar) { FeatureRight = FeatureRights.ViewStaffNotices },
        new("Students", "Academics", "/Students/Index", "Academics", "fa-user-graduate", AccessRights.ViewLearners, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Staff", "Academics", "/Staff/Index", "Academics", "fa-chalkboard-teacher", AccessRights.ViewStaff, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Class teacher assignments", "Academics", "/ClassTeacherAssignments/Index", "Academics", "fa-chalkboard-teacher", AccessRights.ViewClasses, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Subject assignments", "Academics", "/SubjectAssignments/Index", "Academics", "fa-book", AccessRights.ViewSubjects, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Prefects", "Academics", "/Prefects/Index", "Academics", "fa-user-tie", AccessRights.ViewPrefects, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Nominate prefects", "Academics", "/Prefects/Nominate", "Academics", "fa-user-check", AccessRights.None, SchoolWorkspace.Administration, SchoolWorkspace.Teacher) { RequiresNominationPermission = true },
        new("Prefect nominations", "Academics", "/Prefects/Nominations", "Academics", "fa-user-check", AccessRights.ViewPrefects, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Classes", "Config", "/Classes/Index", "Academics", "fa-school", AccessRights.ViewClasses, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Subjects", "Config", "/Subjects/Index", "Academics", "fa-book", AccessRights.ViewSubjects, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Grades", "Config", "/Grades/Index", "School setup", "fa-layer-group", AccessRights.ViewGrades, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Terms", "Config", "/Terms/Index", "School setup", "fa-calendar-alt", AccessRights.ViewTerms, SchoolWorkspace.Administration, SchoolWorkspace.Bursar, SchoolWorkspace.Teacher),
        new("House assignments", "Sports", "/Houses/Index", "Sports & houses", "fa-users", AccessRights.ViewHouses, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Sports", "Sports", "/Sporting/Index", "Sports & houses", "fa-futbol", AccessRights.ViewSports, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Fees overview", "Finance", "/Fees/Index", "Finance", "fa-wallet", AccessRights.ViewFeeBalances, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("Student balances", "Finance", "/Fees/Balances", "Finance", "fa-file-invoice-dollar", AccessRights.ViewFeeBalances, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("Cash-up", "Finance", "/Fees/CashUp", "Finance", "fa-cash-register", AccessRights.ViewFeeBalances, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("Fee structures", "Finance", "/Fees/FeesStructures", "Finance", "fa-list", AccessRights.ViewFeeBalances, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("Open term", "Finance", "/Fees/OpenTerm", "Finance", "fa-calendar-plus", AccessRights.GenerateInvoices, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("Currencies", "Config", "/Currency/Index", "Finance", "fa-coins", AccessRights.ViewCurrencies, SchoolWorkspace.Administration, SchoolWorkspace.Bursar),
        new("School gallery", "Config", "/Gallery/Index", "Administration", "fa-images", AccessRights.None, SchoolWorkspace.Administration) { FeatureRight = FeatureRights.ViewGallery },
        new("Mass timetable", "Faith", "/Masses/Index", "Faith", "fa-cross", AccessRights.None, SchoolWorkspace.Administration, SchoolWorkspace.Teacher) { FeatureRight = FeatureRights.ViewMasses },
        new("Users", "Config", "/Users/Index", "Administration", "fa-user-shield", AccessRights.ViewUsers, SchoolWorkspace.Administration),
        new("User groups", "Config", "/UserGroups/Index", "Administration", "fa-users-cog", AccessRights.ViewUserGroups, SchoolWorkspace.Administration),
        new("Sport types", "Config", "/Sports/Index", "School setup", "fa-futbol", AccessRights.ViewSports, SchoolWorkspace.Administration),
        new("Houses", "Config", "/Houses/Index", "School setup", "fa-home", AccessRights.ViewHouses, SchoolWorkspace.Administration),
        new("Villages", "Config", "/Villages/Index", "School setup", "fa-map-marker-alt", AccessRights.ViewVillages, SchoolWorkspace.Administration, SchoolWorkspace.Teacher),
        new("Audit logs", "Config", "/AuditLogs/Index", "Administration", "fa-history", AccessRights.ViewAuditLogs, SchoolWorkspace.Administration)
    ];

    public bool Can(AccessRights right) => user.IsAuthenticated && Workspace != SchoolWorkspace.Unassigned && user.HasRight(right);
    public bool Can(FeatureRights right) => user.IsAuthenticated && Workspace != SchoolWorkspace.Unassigned && user.HasFeatureRight(right);
    public bool CanNominatePrefects => Can(AccessRights.None) && user.CanNominatePrefects && Workspace is SchoolWorkspace.Administration or SchoolWorkspace.Teacher;
    private bool CanUseFeature(WorkspaceFeature feature) => (feature.RequiresNominationPermission ? CanNominatePrefects : Can(feature.Right)) && Can(feature.FeatureRight);
    public bool Applicable(WorkspaceFeature feature) => feature.Workspaces.Contains(Workspace);
    public IEnumerable<WorkspaceFeature> Navigation => Features.Where(f => Applicable(f) && CanUseFeature(f));

    public bool CanOpen(string area, string page)
    {
        var feature = Features.FirstOrDefault(f => f.Area.Equals(area, StringComparison.OrdinalIgnoreCase)
            && f.Page.Equals(page, StringComparison.OrdinalIgnoreCase));
        return feature != null && Applicable(feature) && CanUseFeature(feature);
    }

    public bool IsRelevantPage(string area, string page)
    {
        if (area.Equals("Finance", StringComparison.OrdinalIgnoreCase))
            return Workspace is SchoolWorkspace.Administration or SchoolWorkspace.Bursar;
        var folder = page[..(page.LastIndexOf('/') + 1)];
        return Features.Any(f => f.Area.Equals(area, StringComparison.OrdinalIgnoreCase)
            && f.Page.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && Applicable(f));
    }
}
