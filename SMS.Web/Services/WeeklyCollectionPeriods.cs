namespace SMS.Web.Services;

public static class WeeklyCollectionPeriods
{
    public record Period(DateTime Start, DateTime End)
    {
        public string Label => Start == End ? Start.ToString("dd MMM") : $"{Start:dd}–{End:dd MMM}";
    }

    public static List<Period> Create(DateTime from, DateTime to)
    {
        from = from.Date;
        to = to.Date;
        if (to < from || from.Year != to.Year || from.Month != to.Month)
            throw new ArgumentException("Weekly collections must be within one month.");
        var monthStart = new DateTime(from.Year, from.Month, 1);
        var periods = new List<Period>();
        for (var start = monthStart; start <= to; start = start.AddDays(7))
        {
            var end = start.AddDays(6);
            if (end > to) end = to;
            if (end < from) continue;
            periods.Add(new Period(start < from ? from : start, end));
        }
        return periods;
    }
}
