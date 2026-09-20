using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Data;
using Microsoft.EntityFrameworkCore;
using SMS.Web.Areas.Config.Pages.UserGroups.ViewModels;

namespace SMS.Web.Areas.Config.Pages.UserGroups
{
    public class UpsertModel : PageModel
    {
        private readonly SMSDbContext Db;

        public UpsertModel(SMSDbContext context)
        {
            Db = context;
        }

        public UserGroup Group { get; set; }

        [BindProperty]
        public UserGroup UserGroupVM { get; set; }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            Group = await Db.UserGroups.FirstOrDefaultAsync();
            return Page();
        }
    }
}
