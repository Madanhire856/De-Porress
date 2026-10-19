namespace SMS.Web.Services;

public sealed class CognitoSettings
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Domain { get; set; } = "";
    public string LogoutUri { get; set; } = "";
    public bool InvitationsEnabled { get; set; }
    public string UserPoolId => new Uri(Authority).AbsolutePath.Trim('/');
    public string Region => UserPoolId.Split('_')[0];

    public bool HasInvitationAuthority()
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out var uri) || uri.Scheme != "https") return false;
        var pool = uri.AbsolutePath.Trim('/');
        var separator = pool.IndexOf('_');
        return separator > 0 && separator < pool.Length - 1 && !pool.Contains('/')
            && (uri.Host == $"cognito-idp.{pool[..separator]}.amazonaws.com"
                || uri.Host == $"cognito-idp.{pool[..separator]}.amazonaws.com.cn");
    }

    public static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.Query);
}
