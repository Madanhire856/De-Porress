using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace SMS.Web.Services;

public static class CognitoEmailVerification
{
    public const string Scheme = "EmailVerification";
    public const string TokenKey = "cognito_verification_access_token";
    public const string ReturnKey = "verification_return_url";

    // Run after the OIDC handler has completed token/nonce/protocol validation.
    public static async Task TicketReceivedAsync(TicketReceivedContext context)
    {
        string? accessToken = null;
        context.Properties?.Items.Remove(TokenKey, out accessToken);
        if (CognitoIdentity.HasVerifiedEmail(context.Principal))
        {
            await context.HttpContext.SignOutAsync(Scheme);
            return;
        }
        var subject = context.Principal?.FindFirst("sub")?.Value;
        var email = context.Principal?.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
        {
            context.Fail("A verified staff email address is required.");
            return;
        }
        // A separate protected cookie is valid only on the email-verification page.
        // No app session, roles or workspace access are issued to this principal.
        var pending = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", subject), new Claim("email", email), new Claim(TokenKey, accessToken)
        ], Scheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            AllowRefresh = false
        };
        properties.Items[ReturnKey] = SafeReturnUrl(context.ReturnUri);
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.HttpContext.SignInAsync(Scheme, pending, properties);
        context.Response.Redirect("/Identity/Account/VerifyEmail");
        context.HandleResponse();
    }

    public static string SafeReturnUrl(string? value) => value is { Length: > 0 }
        && value[0] == '/' && (value.Length == 1 || value[1] is not ('/' or '\\'))
        && !value.Any(char.IsControl) ? value : "/";
}
