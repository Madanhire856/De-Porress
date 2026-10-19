namespace SMS.Web.Pages.Shared.Tables;

public record TableCount(int Count, string Singular, string? Plural = null)
{
    public string Label => Count == 1 ? Singular : Plural ?? Singular + "s";
}
