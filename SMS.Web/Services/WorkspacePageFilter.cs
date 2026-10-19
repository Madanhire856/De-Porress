using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SMS.Web.Services;

public class WorkspacePageFilter(WorkspaceAccess access) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"]?.ToString() ?? "";
        var page = context.RouteData.Values["page"]?.ToString() ?? "";
        if (area is not ("Academics" or "Config" or "Sports" or "Finance" or "Faith"))
        {
            await next();
            return;
        }
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }
        if (!access.IsRelevantPage(area, page))
        {
            context.Result = new ForbidResult();
            return;
        }
        // Existing create/edit/payment handlers retain their operation-specific checks.
        var action = page[(page.LastIndexOf('/') + 1)..];
        if (action is "Index" or "Details" or "Board" or "Balances" or "Ledger" or "CashUp" or "FeesStructures" or "FeesStructureDetails")
        {
            var feature = WorkspaceAccess.Features.FirstOrDefault(f => f.Area.Equals(area, StringComparison.OrdinalIgnoreCase)
                && f.Page.Equals(page, StringComparison.OrdinalIgnoreCase))
                ?? WorkspaceAccess.Features.FirstOrDefault(f => f.Area.Equals(area, StringComparison.OrdinalIgnoreCase)
                    && f.Page.Equals(page[..(page.LastIndexOf('/') + 1)] + "Index", StringComparison.OrdinalIgnoreCase));
            if (feature == null || !access.Can(feature.Right) || !access.Can(feature.FeatureRight))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        await next();
    }
}
