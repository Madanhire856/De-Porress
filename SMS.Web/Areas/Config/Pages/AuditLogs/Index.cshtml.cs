using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.AuditLogs
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public IndexModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string? Entity { get; set; }
        [BindProperty(SupportsGet = true)] public string? User { get; set; }
        [BindProperty(SupportsGet = true)] public string? Action { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? EntityId { get; set; }

        public List<RowVM> Rows { get; set; } = new();
        public List<string> AvailableEntities { get; set; } = new();
        public List<string> AvailableUsers { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();

        public int TotalCount { get; set; }

        public class RowVM
        {
            public Guid Id { get; set; }
            public DateTime TimeStamp { get; set; }
            public string? Username { get; set; }
            public string EntityType { get; set; } = "";
            public Guid EntityId { get; set; }
            public string Action { get; set; } = "";
            public string? IpAddress { get; set; }
            public string? BeforeValue { get; set; }
            public string? AfterValue { get; set; }
            public string? Reason { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Only users with the specific audit-log right can view it
            if (!_currentUser.HasRight(AccessRights.ViewAuditLogs))
                return Forbid();

            // Default range: last 7 days
            if (!From.HasValue)
                From = DateTime.Today.AddDays(-7);
            if (!To.HasValue)
                To = DateTime.Today.AddDays(1);

            // Lookups for filter dropdowns
            AvailableEntities = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            AvailableUsers = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Where(a => a.Username != null)
                .Select(a => a.Username!)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            AvailableActions = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Build query
            var query = _context.Set<AuditLog>()
                .AsNoTracking()
                .Where(a => a.TimeStamp >= From.Value
                         && a.TimeStamp < To.Value);

            if (!string.IsNullOrWhiteSpace(Entity))
                query = query.Where(a => a.EntityType == Entity);

            if (!string.IsNullOrWhiteSpace(User))
                query = query.Where(a => a.Username == User);

            if (!string.IsNullOrWhiteSpace(Action))
                query = query.Where(a => a.Action == Action);

            if (EntityId.HasValue && EntityId.Value != Guid.Empty)
                query = query.Where(a => a.EntityId == EntityId.Value);

            TotalCount = await query.CountAsync();

            Rows = await query
                .OrderByDescending(a => a.TimeStamp)
                .Take(500)   
                .Select(a => new RowVM
                {
                    Id = a.Id,
                    TimeStamp = a.TimeStamp,
                    Username = a.Username,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Action = a.Action,
                    IpAddress = a.IpAddress,
                    BeforeValue = a.BeforeValue,
                    AfterValue = a.AfterValue,
                    Reason = a.Reason
                })
                .ToListAsync();

            return Page();
        }
    }
}