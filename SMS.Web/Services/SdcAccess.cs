using SMS.Lib;
namespace SMS.Web.Services;
public static class SdcAccess
{
    public static bool CanViewMembers(WorkspaceAccess access) => access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.ViewSdcMembers);
    public static bool CanCreateMembers(WorkspaceAccess access) => CanViewMembers(access) && access.Can(FeatureRights.CreateSdcMembers);
    public static bool CanEditMembers(WorkspaceAccess access) => CanViewMembers(access) && access.Can(FeatureRights.EditSdcMembers);
    public static bool CanViewPositions(WorkspaceAccess access) => access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.ViewSdcPositions);
    public static bool CanCreatePositions(WorkspaceAccess access) => CanViewPositions(access) && access.Can(FeatureRights.CreateSdcPositions);
    public static bool CanEditPositions(WorkspaceAccess access) => CanViewPositions(access) && access.Can(FeatureRights.EditSdcPositions);
    public static bool CanViewTerms(WorkspaceAccess access) => access.Workspace == SchoolWorkspace.Administration && access.Can(FeatureRights.ViewSdcTerms);
    public static bool CanCreateTerms(WorkspaceAccess access) => CanViewTerms(access) && access.Can(FeatureRights.CreateSdcTerms);
    public static bool CanEditTerms(WorkspaceAccess access) => CanViewTerms(access) && access.Can(FeatureRights.EditSdcTerms);
}
