using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Houses
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public House HouseVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public string? MasterName { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            HouseVM = _context.Houses
                .Include(h => h.Creator)
                .Include(h => h.Master)
                .FirstOrDefault(h => h.Id == id);

            if (HouseVM == null)
                return NotFound();

            _context.ApplyHouseNumber(HouseVM);

            CreatorEmail = HouseVM.Creator?.Email;
            MasterName = HouseVM.Master != null
                ? $"{HouseVM.Master.Name} {HouseVM.Master.Surname}"
                : null;

            return Page();
        }
    }
}