using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Villages
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Village VillageVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public int StudentCount { get; set; }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            VillageVM = _context.Villages
                .Include(v => v.Creator)
                .FirstOrDefault(v => v.Id == id);

            if (VillageVM == null)
                return NotFound();

 
            _context.ApplyVillageNumber(VillageVM);

            CreatorEmail = VillageVM.Creator?.Email;

            StudentCount = _context.Students
                .Count(s => s.VillageId.HasValue && s.VillageId.Value == VillageVM.Id);

            return Page();
        }
    }
}