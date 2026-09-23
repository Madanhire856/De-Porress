using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Users
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public User UserVM { get; set; } = null!;

        public string? CreatorEmail { get; set; }
        public string? GroupName { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            UserVM = _context.Users
                .FirstOrDefault(u => u.Id == id);

            if (UserVM == null)
                return NotFound();
            _context.ApplyUserNumber(UserVM);
            if (UserVM.CreatorId.HasValue)
            {
                CreatorEmail = _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == UserVM.CreatorId.Value)
                    .Select(u => u.Email)
                    .FirstOrDefault();
            }

            // Resolve group name if assigned
            if (UserVM.GroupId.HasValue)
            {
                GroupName = _context.UserGroups
                    .AsNoTracking()
                    .Where(g => g.Id == UserVM.GroupId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefault();
            }

            return Page();
        }
    }
}