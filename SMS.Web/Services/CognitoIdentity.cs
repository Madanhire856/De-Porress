using System.Security.Claims;

namespace SMS.Web.Services;

public static class CognitoIdentity
{
    // Called only after OIDC has validated the ID token's issuer, audience and signature.
    public static bool HasVerifiedEmail(ClaimsPrincipal? principal) =>
        string.Equals(principal?.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(principal?.FindFirst("email")?.Value);
}
