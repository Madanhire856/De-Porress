using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SMS.Data;
using SMS.Lib;

namespace SMS.Web.Services
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private static readonly AsyncLocal<List<AuditLog>?> _pendingAuditRows = new();

        public AuditInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

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
            nameof(UserGroup),
            nameof(User),
        };

        private static readonly HashSet<string> NoisyFields = new(StringComparer.Ordinal)
        {
            "LastLoginDate",
            "ActivationDate",
            "LockoutExpiryDate",
            "SecurityStamp",
            "AuthenticatorKey",
            "AuthRecoveryCodes",
            "IsEmailConfirmed",
            "PasswordHash",
            "TwoFactorAuthEnabled",
        };

        private static readonly HashSet<string> DisplayHiddenFields = new(StringComparer.Ordinal)
        {
            "Id",
            "CreatorId",
            "Creator",
            "CreationDate",
            "LastLoginDate",
            "ActivationDate",
            "LockoutExpiryDate",
            "SecurityStamp",
            "AuthenticatorKey",
            "AuthRecoveryCodes",
            "IsEmailConfirmed",
            "PasswordHash",
            "TwoFactorAuthEnabled",
        };

        // =========================================================
        //  ENUM RESOLVERS
        //  Map (EntityType.FieldName) → int → human-readable name.
        //  When a field is resolved, the " Id" suffix is dropped
        //  from the label — "Category Id: 3 → 2" becomes
        //  "Category: Core → Co-Curricular".
        // =========================================================
        private static readonly Dictionary<string, Func<int, string>> EnumResolvers = new(StringComparer.Ordinal)
        {
            // ---------- SUBJECT ----------
            ["Subject.CategoryId"] = v => ((SubjectCategory)v).ToDisplayName(),

            // ---------- STUDENT ----------
            ["Student.GenderId"] = v => ((Gender)v).ToDisplayName(),
            ["Student.CategoryId"] = v => ((StudentCategory)v).ToDisplayName(),
            ["Student.EnrolmentStatusId"] = v => ((EnrolmentStatus)v).ToDisplayName(),
            ["Student.BaptismStatusId"] = v => ((BaptismStatus)v).ToDisplayName(),

            // ---------- STAFF ----------
            ["Staff.GenderId"] = v => ((Gender)v).ToDisplayName(),
            ["Staff.CategoryId"] = v => ((StaffCategory)v).ToDisplayName(),
            ["Staff.MaritalStatusId"] = v => ((MaritalStatus)v).ToDisplayName(),
            ["Staff.QualificationId"] = v => ((Qualification)v).ToDisplayName(),

            // ---------- PAYMENT ----------
            ["Payment.PaymentMethodId"] = v => ((PaymentMethod)v).ToDisplayName(),

            // ---------- STUDENT LEDGER ----------
            ["StudentLedger.StatusId"] = v => ((StudentLedgerStatus)v).ToDisplayName(),

            // ---------- FEE STRUCTURE ----------
            ["FeesStructure.LevyTypeId"] = v => ((LevyType)v).ToDisplayName(),

            // ---------- GRADE ----------
            ["Grade.GradeLevelId"] = v => ((GradeLevel)v).ToDisplayName(),
        };

        // =========================================================
        //  SYNC
        // =========================================================
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            _pendingAuditRows.Value = AddAuditEntries(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            FlushToFile(_pendingAuditRows.Value);
            _pendingAuditRows.Value = null;
            return base.SavedChanges(eventData, result);
        }

        // =========================================================
        //  ASYNC
        // =========================================================
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _pendingAuditRows.Value = AddAuditEntries(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            FlushToFile(_pendingAuditRows.Value);
            _pendingAuditRows.Value = null;
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // =========================================================
        //  DB ROWS
        // =========================================================
        private List<AuditLog> AddAuditEntries(DbContext? context)
        {
            var result = new List<AuditLog>();
            if (context == null) return result;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added
                                  or EntityState.Modified
                                  or EntityState.Deleted)
                .Where(e => e.Entity.GetType().Name != nameof(AuditLog))
                .Where(e => Watched.Contains(e.Entity.GetType().Name))
                .Where(e => !OnlyNoisyFieldsChanged(e))
                .ToList();

            if (entries.Count == 0) return result;

            Guid? userId = null;
            string userName = "System";
            string? ip = null;

            try
            {
                var currentUser = _serviceProvider.GetService<ICurrentUserService>();
                if (currentUser != null)
                {
                    userId = currentUser.UserId;
                    userName = currentUser.Name ?? "System";
                }
            }
            catch { }

            try
            {
                var http = _serviceProvider.GetService<IHttpContextAccessor>();
                ip = http?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            }
            catch { }

            var now = DateTime.Now;

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
                catch { }

                result.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
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

            if (result.Count > 0)
                context.Set<AuditLog>().AddRange(result);

            return result;
        }

        private static bool OnlyNoisyFieldsChanged(EntityEntry entry)
        {
            if (entry.State != EntityState.Modified)
                return false;

            var changed = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (changed.Count == 0)
                return true;

            return changed.All(name => NoisyFields.Contains(name));
        }

        // =========================================================
        //  TEXT FILE WRITE
        // =========================================================
        private void FlushToFile(List<AuditLog>? logs)
        {
            if (logs == null || logs.Count == 0) return;

            try
            {
                var env = _serviceProvider.GetService<IWebHostEnvironment>();
                var root = env?.ContentRootPath ?? Directory.GetCurrentDirectory();

                var folder = Path.Combine(root, "Logs", "Audit");
                Directory.CreateDirectory(folder);

                var now = DateTime.Now;
                var fileName = $"audit-{now:yyyy-MM-dd}.txt";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();

                if (!File.Exists(path))
                {
                    sb.AppendLine(new string('=', 110));
                    sb.AppendLine($"  AUDIT LOG — {now:dddd, dd MMMM yyyy}");
                    sb.AppendLine("  St. Martin De Porres");
                    sb.AppendLine(new string('=', 110));
                    sb.AppendLine();
                }

                foreach (var log in logs)
                    sb.AppendLine(FormatLine(log));

                _fileLock.Wait();
                try
                {
                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch { /* never break the save */ }
        }

        // =========================================================
        //  FORMATTING
        // =========================================================
        private static string FormatLine(AuditLog log)
        {
            var time = log.TimeStamp.ToString("dd MMM yyyy HH:mm:ss");
            var user = string.IsNullOrWhiteSpace(log.Username) ? "System" : log.Username;
            var action = log.Action;
            var entity = FriendlyEntityName(log.EntityType);
            var details = FormatDetails(log);

            return $"{time} | {user} | {action} {entity} | {details}";
        }

        private static string FriendlyEntityName(string type) => type switch
        {
            "BankStatementEntry" => "Bank Statement Line",
            "FeesStructure" => "Fee Structure",
            "StudentLedger" => "Student Ledger",
            "UserGroup" => "User Group",
            _ => SplitCamelCase(type)
        };

        private static string FormatDetails(AuditLog log)
        {
            var source = log.Action == "Deleted" ? log.BeforeValue : log.AfterValue;
            if (string.IsNullOrWhiteSpace(source)) return "—";

            try
            {
                var after = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(source);
                if (after == null || after.Count == 0) return "—";

                var parts = new List<string>();
                var entityType = log.EntityType;

                if (log.Action == "Updated" && !string.IsNullOrWhiteSpace(log.BeforeValue))
                {
                    var before = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.BeforeValue);

                    foreach (var kvp in after)
                    {
                        if (DisplayHiddenFields.Contains(kvp.Key)) continue;

                        var resolverKey = $"{entityType}.{kvp.Key}";
                        var hasResolver = EnumResolvers.ContainsKey(resolverKey);
                        var label = Prettify(kvp.Key, hasResolver);

                        var newVal = FormatValueWithResolver(entityType, kvp.Key, kvp.Value);
                        var oldVal = before != null && before.TryGetValue(kvp.Key, out var o)
                            ? FormatValueWithResolver(entityType, kvp.Key, o)
                            : "?";

                        if (newVal == null && oldVal == null) continue;

                        parts.Add($"{label}: {oldVal ?? "—"} → {newVal ?? "—"}");
                    }
                }
                else
                {
                    foreach (var kvp in after)
                    {
                        if (DisplayHiddenFields.Contains(kvp.Key)) continue;

                        var resolverKey = $"{entityType}.{kvp.Key}";
                        var hasResolver = EnumResolvers.ContainsKey(resolverKey);
                        var label = Prettify(kvp.Key, hasResolver);

                        var val = FormatValueWithResolver(entityType, kvp.Key, kvp.Value);
                        if (val == null) continue;

                        parts.Add($"{label}: {val}");
                    }
                }

                if (parts.Count == 0) return "—";

                var joined = string.Join(", ", parts);
                return joined.Length > 250 ? joined.Substring(0, 250) + "…" : joined;
            }
            catch
            {
                return source;
            }
        }

        // =========================================================
        //  ENUM-AWARE VALUE FORMATTER
        // =========================================================
        private static string? FormatValueWithResolver(string entityType, string fieldName, JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Number) return FormatValue(el);
            if (!el.TryGetInt32(out var intVal)) return FormatValue(el);

            var key = $"{entityType}.{fieldName}";
            if (EnumResolvers.TryGetValue(key, out var resolver))
            {
                try { return resolver(intVal); }
                catch { /* fall through to default */ }
            }

            return FormatValue(el);
        }

        private static string Prettify(string name, bool dropIdSuffix = false)
        {
            var s = SplitCamelCase(name);
            if (dropIdSuffix && s.EndsWith(" Id", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - 3);
            return s;
        }

        private static string SplitCamelCase(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1]))
                    sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static string? FormatValue(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                case JsonValueKind.True: return "Yes";
                case JsonValueKind.False: return "No";

                case JsonValueKind.Number:
                    if (el.TryGetDecimal(out var dec))
                    {
                        // Whole numbers → integer format (no .00)
                        if (dec == Math.Floor(dec))
                            return dec.ToString("N0");

                        // Fractional — preserve scale: 2 decimals for money,
                        // 4 for exchange rates
                        var scale = (decimal.GetBits(dec)[3] >> 16) & 0xFF;
                        return scale > 2
                            ? dec.ToString("N4")
                            : dec.ToString("N2");
                    }
                    return el.GetRawText();

                case JsonValueKind.String:
                    var s = el.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;

                    if (DateTime.TryParse(s, out var dt))
                        return dt.ToString("dd MMM yyyy");

                    return s;

                default:
                    return el.GetRawText();
            }
        }

        // =========================================================
        //  HELPERS
        // =========================================================
        private static Guid GetEntityId(EntityEntry entry)
        {
            var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            return pk?.CurrentValue is Guid g ? g : Guid.Empty;
        }

        private static string Serialise(EntityEntry entry, bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (entry.State == EntityState.Modified && !prop.IsModified)
                    continue;

                dict[prop.Metadata.Name] = useOriginal
                    ? prop.OriginalValue
                    : prop.CurrentValue;
            }

            return JsonSerializer.Serialize(dict);
        }
    }
}