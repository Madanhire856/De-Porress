using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SMS.Data;

namespace SMS.Lib
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SMSDbContext _context;

        // User cache
        private Guid? _cachedUserId;
        private int? _cachedRoleId;
        private Guid? _cachedGroupId;
        private bool _hasLookedUpUser;

        // Rights cache
        private long? _cachedRights;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, SMSDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        // ------------------------------------------------------------
        // Azure AD Claims
        // ------------------------------------------------------------
        public string? Email =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("preferred_username")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public string? Name =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("given_name");

        public string? AzureId =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("oid");

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        private void EnsureUserLoaded()
        {
            if (_hasLookedUpUser) return;

            var email = Email;
            if (string.IsNullOrEmpty(email))
            {
                _hasLookedUpUser = true;
                return;
            }

            var localUser = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email);

            if (localUser != null)
            {
                _cachedUserId = localUser.Id;
                _cachedRoleId = localUser.RoleId;
                _cachedGroupId = localUser.GroupId;
            }

            _hasLookedUpUser = true;
        }

        public Guid? UserId
        {
            get { EnsureUserLoaded(); return _cachedUserId; }
        }

        public int? RoleId
        {
            get { EnsureUserLoaded(); return _cachedRoleId; }
        }

        public Guid? GroupId
        {
            get { EnsureUserLoaded(); return _cachedGroupId; }
        }

        public long Rights
        {
            get
            {
                if (_cachedRights.HasValue) return _cachedRights.Value;

                EnsureUserLoaded();

                long rights = 0;

                // 1. Group rights (bitmask)
                if (_cachedGroupId.HasValue)
                {
                    var groupRights = _context.UserGroups
                        .AsNoTracking()
                        .Where(g => g.Id == _cachedGroupId.Value)
                        .Select(g => g.RightsId)
                        .FirstOrDefault();

                    if (groupRights.HasValue)
                        rights |= groupRights.Value;
                }

                // 2. Administrator override — grants everything
                if (_cachedRoleId == (int)UserRole.ADMIN)
                    rights = ~0L;

                _cachedRights = rights;
                return rights;
            }
        }

        public bool HasRight(AccessRights right)
        {
            if (right == AccessRights.None) return true;
            return (Rights & (long)right) == (long)right;
        }

        public async Task<Guid> GetUserIdAsync()
        {
            EnsureUserLoaded();
            if (_cachedUserId.HasValue)
                return _cachedUserId.Value;

            var email = Email;
            if (string.IsNullOrEmpty(email))
                throw new InvalidOperationException("No authenticated user found.");

            var localUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (localUser == null)
                throw new InvalidOperationException(
                    $"Authenticated user '{email}' does not exist in the local database. " +
                    $"Call EnsureUserExistsAsync() first to auto-provision the account.");

            _cachedUserId = localUser.Id;
            _cachedRoleId = localUser.RoleId;
            _cachedGroupId = localUser.GroupId;
            _hasLookedUpUser = true;
            return localUser.Id;
        }

        // ------------------------------------------------------------
        // EnsureUserExistsAsync (auto-provisioning on first login)
        // ------------------------------------------------------------
        public async Task<Guid?> EnsureUserExistsAsync()
        {
            if (!IsAuthenticated)
                return null;

            if (_hasLookedUpUser && _cachedUserId.HasValue)
                return _cachedUserId.Value;

            var email = Email;
            if (string.IsNullOrEmpty(email))
                return null;

            var localUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (localUser != null)
            {
                localUser.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _cachedUserId = localUser.Id;
                _cachedRoleId = localUser.RoleId;
                _cachedGroupId = localUser.GroupId;
                _hasLookedUpUser = true;
                return localUser.Id;
            }

            // --- New user: provision ---
            bool isFirstUser = !await _context.Users.AnyAsync();
            var role = isFirstUser ? UserRole.ADMIN : UserRole.STAFF;

            var loginId = email.Contains('@')
                ? email.Substring(0, email.IndexOf('@'))
                : email;

            var baseLoginId = loginId;
            var suffix = 1;
            while (await _context.Users.AnyAsync(u => u.LoginId == loginId))
            {
                loginId = $"{baseLoginId}{suffix++}";
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                LoginId = loginId,
                Email = email,
                Name = Name ?? email,
                Mobile = null,
                PasswordHash = null,     
                IsActive = true,
                ActivationDate = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatorId = null,        
                RoleId = (int)role,
                GroupId = null,
                IsEmailConfirmed = true,
                TwoFactorAuthEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                AuthRecoveryCodes = null,
                AuthenticatorKey = null,
                LockoutExpiryDate = null,
                LastLoginDate = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _cachedUserId = newUser.Id;
            _cachedRoleId = newUser.RoleId;
            _cachedGroupId = newUser.GroupId;
            _hasLookedUpUser = true;

            return newUser.Id;
        }
    }
}