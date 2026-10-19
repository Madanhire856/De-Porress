using SMS.Data;
using SMS.Lib;

namespace SMS.Web.Services;

public enum TripStatus { Draft, Planned, Completed, Cancelled }

public static class SchoolTrips
{
    public static bool CanView(WorkspaceAccess access) => access.CanOpen("Academics", "/Trips/Index");
    public static bool CanCreate(WorkspaceAccess access) => CanView(access) && access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.CreateTrips);
    public static bool CanEdit(WorkspaceAccess access) => CanView(access) && access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.EditTrips);
    public static bool CanManage(WorkspaceAccess access) => CanCreate(access) || CanEdit(access);
    public static bool CanViewDrafts(WorkspaceAccess access) => CanView(access) && access.Workspace == SchoolWorkspace.Administration
        && (CanManage(access) || access.Can(FeatureRights.ViewTripDrafts));
    public static IQueryable<SchoolTrip> Visible(SMSDbContext db, bool manage) =>
        db.SchoolTrips.Where(x => manage || x.StatusId != (int)TripStatus.Draft);
    public static string StatusName(int status) => Enum.IsDefined(typeof(TripStatus), status) ? ((TripStatus)status).ToString() : "Unknown";
}
