using SMS.Lib;
namespace SMS.Web.Services;
public static class PrefectLifecycle
{
    public static PrefectStatus EffectiveStatus(PrefectStatus status, DateTime end, DateTime today) =>
        status == PrefectStatus.ACTIVE && end.Date < today.Date ? PrefectStatus.COMPLETED : status;
    public static string Label(PrefectStatus status, DateTime start, DateTime end, DateTime today) =>
        status == PrefectStatus.ACTIVE && start.Date > today.Date ? "Upcoming" : EffectiveStatus(status, end, today).ToDisplayName();
}
