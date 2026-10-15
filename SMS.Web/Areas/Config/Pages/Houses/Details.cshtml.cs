using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using System;
using System.Collections.Generic;
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
        public List<AssignedTeacherRow> AssignedTeachers { get; set; } = new();

        public class AssignedTeacherRow
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public int RoleId { get; set; }

            // 👇 computed from the enum — no hardcoding in the view
            public string RoleName => ((TeacherHouseRole)RoleId).ToDisplayName();
        }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            HouseVM = _context.Houses
                .Include(h => h.Creator)
                .FirstOrDefault(h => h.Id == id);

            if (HouseVM == null)
                return NotFound();

            _context.ApplyHouseNumber(HouseVM);

            CreatorEmail = HouseVM.Creator?.Email;

            // Load assigned teachers from the join table
            AssignedTeachers = _context.HouseTeachers
                .AsNoTracking()
                .Where(ht => ht.HouseId == HouseVM.Id)
                .OrderBy(ht => ht.RoleId)
                .ThenBy(ht => ht.Staff.Surname)
                .ThenBy(ht => ht.Staff.Name)
                .Select(ht => new AssignedTeacherRow
                {
                    StaffId = ht.StaffId,
                    FullName = ht.Staff.Name + " " + ht.Staff.Surname,
                    RoleId = ht.RoleId
                })
                .ToList();

            return Page();
        }
    }
}