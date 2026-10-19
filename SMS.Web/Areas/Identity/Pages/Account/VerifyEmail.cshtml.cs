using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SMS.Web.Services;

namespace SMS.Web.Areas.Identity.Pages.Account;

[Authorize(AuthenticationSchemes = CognitoEmailVerification.Scheme)]
[EnableRateLimiting("email-verification")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class VerifyEmailModel(ICognitoUserService cognito) : PageModel
{
    [BindProperty, Required, RegularExpression("^[0-9]{6}$", ErrorMessage = "Enter the six-digit code from your email.")]
    public string Code { get; set; } = "";
    public string Email => User.FindFirst("email")?.Value ?? "";
    [TempData] public string? StatusMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostSendAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        var ticket = await HttpContext.AuthenticateAsync(CognitoEmailVerification.Scheme);
        if (!ticket.Succeeded || ticket.Properties == null) return Restart();
        if (ticket.Properties.Items.TryGetValue("verification_last_sent", out var value)
            && DateTimeOffset.TryParse(value, out var lastSent)
            && DateTimeOffset.UtcNow - lastSent < TimeSpan.FromSeconds(60))
        {
            ModelState.AddModelError("", "Please wait one minute before requesting another code.");
            return Page();
        }
        try
        {
            await cognito.SendVerificationCodeAsync(Token(), Subject(), Email, cancellationToken);
            ticket.Properties.Items["verification_last_sent"] = DateTimeOffset.UtcNow.ToString("O");
            await HttpContext.SignInAsync(CognitoEmailVerification.Scheme, ticket.Principal!, ticket.Properties);
            StatusMessage = "A verification code has been sent to your email. Check your spam folder too.";
            return RedirectToPage();
        }
        catch (CognitoUserOperationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            var ticket = await HttpContext.AuthenticateAsync(CognitoEmailVerification.Scheme);
            if (!ticket.Succeeded) return Restart();
            await cognito.VerifyEmailAsync(Token(), Subject(), Email, Code, cancellationToken);
            string? destination = null;
            ticket.Properties?.Items.TryGetValue(CognitoEmailVerification.ReturnKey, out destination);
            await HttpContext.SignOutAsync(CognitoEmailVerification.Scheme);
            // Obtain a fresh validated ID token containing email_verified=true.
            // Never manufacture a normal app session from the temporary cookie.
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = CognitoEmailVerification.SafeReturnUrl(destination)
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
        catch (CognitoUserOperationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }

    private string Token() => User.FindFirst(CognitoEmailVerification.TokenKey)?.Value
        ?? throw new CognitoUserOperationException("Your verification session expired. Start sign-in again.");
    private string Subject() => User.FindFirst("sub")?.Value
        ?? throw new CognitoUserOperationException("Your verification session expired. Start sign-in again.");
    private IActionResult Restart() => RedirectToPage("/Account/StaffLogin", new { area = "Identity" });
}
