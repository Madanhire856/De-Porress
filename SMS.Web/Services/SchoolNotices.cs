using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;

namespace SMS.Web.Services;

public static class SchoolNotices
{
    public static bool CanManage(WorkspaceAccess access) => access.Workspace == SchoolWorkspace.Administration
        && access.Can(FeatureRights.ViewNotices);
    public static bool CanCreate(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.CreateNotices);
    public static bool CanEdit(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.EditNotices);
    public static bool CanPublish(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.PublishNotices);
    public static IQueryable<Announcement> Published(SMSDbContext db) => db.Announcements.AsNoTracking()
        .Where(x => x.ChannelId == (int)AnnouncementChannel.WEBSITE && x.StatusId == (int)AnnouncementStatus.SENT);
    public static bool CanReadStaff(WorkspaceAccess access) => access.Workspace != SchoolWorkspace.Unassigned
        && access.Can(FeatureRights.ViewStaffNotices);
    public static IQueryable<Announcement> StaffPublished(SMSDbContext db) => db.Announcements.AsNoTracking()
        .Where(x => x.ChannelId == (int)AnnouncementChannel.STAFF_NOTICE && x.StatusId == (int)AnnouncementStatus.SENT);
    public static IQueryable<Announcement> Managed(SMSDbContext db) => db.Announcements
        .Where(x => x.ChannelId == (int)AnnouncementChannel.WEBSITE || x.ChannelId == (int)AnnouncementChannel.STAFF_NOTICE);
    public static string Preview(string? body)
    {
        var text = (body ?? "").Replace("\r", " ").Replace("\n", " ");
        return text.Length <= 180 ? text : text[..180] + "…";
    }
}
