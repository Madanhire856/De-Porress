namespace SMS.Web.Services;

public sealed class CognitoSettings
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Domain { get; set; } = "";
    public string LogoutUri { get; set; } = "";

    public static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.Query);
}
