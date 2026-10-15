using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMS.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("User logged out via POST.");

            // 1. Clear all local application cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Clear the Cognito managed-login session.
            //    Redirect through Cognito's managed-login logout endpoint.
            var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);

            AddNoCacheHeaders();
            return new EmptyResult();
        }

        public IActionResult OnGet() => StatusCode(StatusCodes.Status405MethodNotAllowed);

        private void AddNoCacheHeaders()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
        }
    }
}
