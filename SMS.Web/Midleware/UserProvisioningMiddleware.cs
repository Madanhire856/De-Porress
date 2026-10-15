using Microsoft.AspNetCore.Http;
using SMS.Lib;
using System.Threading.Tasks;

namespace SMS.Web.Middleware
{
    public class UserProvisioningMiddleware
    {
        private readonly RequestDelegate _next;

        public UserProvisioningMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
        {
            // Only run for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // Auto-create the local User row on first login
                    await currentUser.EnsureUserExistsAsync();
                }
                catch (Exception ex)
                {
                    // Log but don't block the request — the user can still browse
                    var logger = context.RequestServices
                        .GetRequiredService<ILogger<UserProvisioningMiddleware>>();
                    logger.LogError(ex, "Auto-provisioning failed for {Email}", currentUser.Email);
                }
            }

            await _next(context);
        }
    }
}