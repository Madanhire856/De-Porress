using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SMS.Lib;
using SMS.Data;

namespace SMS.Web
{
    /// <summary>
    /// Watches SaveChanges/SaveChangesAsync and writes one AuditLog row
    /// per entity change, with before/after values as JSON.
    /// Registration: Program.cs (scoped) + AddInterceptors.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(
            ICurrentUserService currentUser,
            IHttpContextAccessor httpContextAccessor)
        {
            _currentUser = currentUser;
            _httpContextAccessor = httpContextAccessor;
        }

        // =========================================================
        //  Entities that get audited. Add new entity class names
        //  here as the system grows.
        // =========================================================
        private static readonly HashSet<string> Watched = new(StringComparer.Ordinal)
        {
            nameof(Payment),
            nameof(BankStatementEntry),
            nameof(FeesStructure),
            nameof(StudentLedger),
            nameof(Currency),
            nameof(Term),
            nameof(Grade),
            nameof(Class),
            nameof(Subject),
            nameof(Village),
            nameof(House),
            nameof(Sport),
            nameof(Student),
            nameof(Staff),
            nameof(User),
            nameof(UserGroup),
        };

        // ---- Sync ----
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            AddAuditEntries(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        // ---- Async ----
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AddAuditEntries(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // =========================================================
        //  Core
        // =========================================================
        private void AddAuditEntries(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added
                                  or EntityState.Modified
                                  or EntityState.Deleted)
                .Where(e => Watched.Contains(e.Entity.GetType().Name))
                .ToList();

            if (entries.Count == 0) return;

            // Resolve user info once for the whole batch
            Guid? userId = null;
            string userName = "System";
            string? ip = null;

            try
            {
                userId = _currentUser.UserId;
                userName = _currentUser.Name ?? "System";
            }
            catch
            {
                // Background job / migration — no user context
            }

            try
            {
                ip = _httpContextAccessor.HttpContext?
                        .Connection?
                        .RemoteIpAddress?
                        .ToString();
            }
            catch { /* ignore */ }

            var now = DateTime.Now;
            var logs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                var entityId = GetEntityId(entry);
                if (entityId == Guid.Empty) continue;

                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                };

                string? before = null;
                string? after = null;

                try
                {
                    if (entry.State != EntityState.Added)
                        before = Serialise(entry, useOriginal: true);

                    if (entry.State != EntityState.Deleted)
                        after = Serialise(entry, useOriginal: false);
                }
                catch
                {
                    // Serialisation is best-effort — never block the save.
                }

                logs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = (Guid)userId,
                    Username = userName,
                    EntityType = entityName,
                    EntityId = entityId,
                    Action = action,
                    BeforeValue = before,
                    AfterValue = after,
                    TimeStamp = now,
                    IpAddress = ip
                });
            }

            if (logs.Count > 0)
                context.Set<AuditLog>().AddRange(logs);
        }

        // =========================================================
        //  Helpers
        // =========================================================
        private static Guid GetEntityId(EntityEntry entry)
        {
            var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());

            if (pk?.CurrentValue is Guid g)
                return g;

            // Non-Guid PKs (e.g. Currency.Code is a string) — hash isn't
            // appropriate here, so we return Empty and skip. Extend later
            // if you want to audit entities keyed by strings.
            return Guid.Empty;
        }

        private static string Serialise(EntityEntry entry, bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                // For updates, only record what actually changed.
                if (entry.State == EntityState.Modified && !prop.IsModified)
                    continue;

                var value = useOriginal
                    ? prop.OriginalValue
                    : prop.CurrentValue;

                dict[prop.Metadata.Name] = value;
            }

            return JsonSerializer.Serialize(dict);
        }
    }
}