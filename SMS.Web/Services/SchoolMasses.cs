using SMS.Lib;

namespace SMS.Web.Services;

public static class SchoolMasses
{
    public static bool CanView(WorkspaceAccess access) => access.CanOpen("Faith", "/Masses/Index");
    public static bool CanCreate(WorkspaceAccess access) => CanView(access)
        && access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.CreateMasses);
    public static bool CanEdit(WorkspaceAccess access) => CanView(access)
        && access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.EditMasses);
    public static string RoleName(int role) => role switch {
        (int)MinistryRole.ALTER_SERVER => "Altar server", (int)MinistryRole.READER => "Reader", _ => "Other ministry"
    };
}
