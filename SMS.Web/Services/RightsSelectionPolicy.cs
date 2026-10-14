using SMS.Lib;
namespace SMS.Web.Services;
public static class RightsSelectionPolicy
{
    public static bool IsValid(long value) => value != 0 && Enum.IsDefined(typeof(AccessRights), value);
}
