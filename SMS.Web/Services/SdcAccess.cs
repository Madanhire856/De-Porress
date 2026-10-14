using SMS.Lib;
namespace SMS.Web.Services;
public static class SdcAccess
{
    public static bool CanManage(WorkspaceAccess access) => access.Workspace == SchoolWorkspace.Administration
        && access.Can(AccessRights.None);
}
