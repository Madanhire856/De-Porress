using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Staff
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Data.Staff StaffVM { get; set; } = null!;
        public string? LinkedUserEmail { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            StaffVM = _context.Staff
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (StaffVM == null)
                return NotFound();

            // Resolve the linked user's email (optional field)
            if (StaffVM.UserId.HasValue)
            {
                LinkedUserEmail = StaffVM.User?.Email
                    ?? _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == StaffVM.UserId.Value)
                        .Select(u => u.Email)
                        .FirstOrDefault();
            }

            return Page();
        }
    }
}