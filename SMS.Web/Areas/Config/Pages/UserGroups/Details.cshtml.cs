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
                    Total = cat.Rights.Count
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

                RightsByCategory.Add(group);
            }

            return Page();
        }
    }
}