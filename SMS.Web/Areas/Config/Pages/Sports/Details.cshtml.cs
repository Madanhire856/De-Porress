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

namespace SMS.Web.Areas.Config.Pages.Sports
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Sport SportVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }
        public List<AssignedCoachRow> AssignedCoaches { get; set; } = new();

        public class AssignedCoachRow
        {
            public Guid StaffId { get; set; }
            public string FullName { get; set; } = "";
            public int RoleId { get; set; }

            public string RoleName => ((TeacherSportRole)RoleId).ToDisplayName();
        }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            SportVM = _context.Sports
                .Include(s => s.Creator)
                .FirstOrDefault(s => s.Id == id);

            if (SportVM == null)
                return NotFound();

            _context.ApplySportNumber(SportVM);

            CreatorEmail = SportVM.Creator?.Email;

            AssignedCoaches = _context.SportTeachers
                .AsNoTracking()
                .Where(st => st.SportId == SportVM.Id)
                .OrderBy(st => st.RoleId)
                .ThenBy(st => st.Staff.Surname)
                .ThenBy(st => st.Staff.Name)
                .Select(st => new AssignedCoachRow
                {
                    StaffId = st.StaffId,
                    FullName = st.Staff.Name + " " + st.Staff.Surname,
                    RoleId = st.RoleId
                })
                .ToList();

            return Page();
        }
    }
}