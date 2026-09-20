using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMS.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class StaffLoginModel : PageModel
    {
        public IActionResult OnGet(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };

            properties.SetParameter("prompt", "select_account");

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}