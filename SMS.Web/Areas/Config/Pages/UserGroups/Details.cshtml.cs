using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.UserGroups
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public UserGroup UserGroup { get; set; } = null!;

        public List<CategoryGroup> RightsByCategory { get; set; } = new();
        public int TotalRightsGranted { get; set; }
        public int TotalPossibleRights { get; set; }
        public bool UsesLegacyFeatureRights => UserGroup.FeatureRightsId == null;

        // Helper class for grouping the granted rights per category
        public class CategoryGroup
        {
            public string Title { get; set; } = "";
            public string Icon { get; set; } = "fa-circle";
            public int Total { get; set; }
            public List<string> Granted { get; set; } = new();
        }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            UserGroup = _context.UserGroups
                .Include(g => g.Creator)
                .FirstOrDefault(g => g.Id == id);

            if (UserGroup == null)
                return NotFound();

            // Build the rights-by-category view
            long rights = UserGroup.RightsId ?? 0;
            var categories = RightsCatalog.GetCategories();

            foreach (var cat in categories)
            {
                var group = new CategoryGroup
                {
                    Title = cat.Title,
                    Icon = cat.Icon,
                    Total = cat.Rights.Count + (cat.Title == "Prefects" ? 1 : 0)
                };

                foreach (var right in cat.Rights)
                {
                    TotalPossibleRights++;
                    if ((rights & (long)right.Right) == (long)right.Right)
                    {
                        group.Granted.Add(right.DisplayName);
                        TotalRightsGranted++;
                    }
                }
                if (cat.Title == "Prefects")
                {
                    TotalPossibleRights++;
                    if (UserGroup.CanNominatePrefects) { group.Granted.Add("Nominate Prefects"); TotalRightsGranted++; }
                }

                RightsByCategory.Add(group);
            }

            var featureRights = UserGroup.FeatureRightsId ?? FeatureRightsCatalog.Legacy(rights);
            foreach (var category in FeatureRightsCatalog.Categories)
            {
                var group = new CategoryGroup { Title = category.Title, Icon = category.Icon, Total = category.Rights.Length };
                foreach (var right in category.Rights)
                {
                    TotalPossibleRights++;
                    if ((featureRights & (long)right.Right) == (long)right.Right)
                    { group.Granted.Add(right.DisplayName); TotalRightsGranted++; }
                }
                RightsByCategory.Add(group);
            }
            return Page();
        }
    }
}
