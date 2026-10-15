using SMS.Lib;
namespace SMS.Web.Services;
public static class RoleAssignmentPolicy
{
    public static bool IsSystemAdministrator(int? role) => ((role ?? 0) & (int)UserRole.ADMIN) != 0;
    public static bool CanAssign(int? actorRole, int assignedRole) => !IsSystemAdministrator(assignedRole) || IsSystemAdministrator(actorRole);
    public static bool IsDefined(int role)
    {
        var mask = Enum.GetValues<UserRole>().Aggregate(0, (value, item) => value | (int)item);
        return (role & ~mask) == 0;
    }
}
