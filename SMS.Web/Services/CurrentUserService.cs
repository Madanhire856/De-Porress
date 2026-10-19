using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using Microsoft.Extensions.Configuration;

namespace SMS.Lib
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SMSDbContext _context;
        private readonly IConfiguration? _configuration;

        // User cache
        private Guid? _cachedUserId;
        private string? _cachedName;
        private int? _cachedRoleId;
        private Guid? _cachedGroupId;
        private bool _hasLookedUpUser;
        private bool _cachedIsActive;
        private bool? _cachedCanNominatePrefects;

        // Rights cache
        private long? _cachedRights;
        private long? _cachedFeatureRights;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, SMSDbContext context, IConfiguration? configuration = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _configuration = configuration;
        }

        // ------------------------------------------------------------
        // Cognito claims
        // ------------------------------------------------------------
        public string? Email =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("email")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public string? Name
        {
            get
            {
                if (IsAuthenticated) EnsureUserLoaded();
                return _cachedName ?? ClaimName;
            }
        }

        private string? ClaimName =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("given_name")
            ?? Email;

        public string? IdentitySubject =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
            && SMS.Web.Services.CognitoIdentity.HasVerifiedEmail(_httpContextAccessor.HttpContext.User);

        private void EnsureUserLoaded()
        {
            // Only skip if we've successfully found and cached the user
            if (_hasLookedUpUser && _cachedUserId.HasValue) return;

            var email = Email;
            if (string.IsNullOrEmpty(email))
            {
                _hasLookedUpUser = true;
                return;
            }

            var localUser = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (localUser != null)
            {
                _cachedUserId = localUser.Id;
                _cachedName = localUser.Name;
                _cachedIsActive = localUser.IsActive;
                _cachedRoleId = localUser.IsActive ? localUser.RoleId : 0;
                _cachedGroupId = localUser.IsActive ? localUser.GroupId : null;
                _hasLookedUpUser = true;
            }
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
                if (((_cachedRoleId ?? 0) & (int)UserRole.ADMIN) != 0)
                    rights = ~0L;

                _cachedRights = rights;
                return rights;
            }
        }

        public bool HasRight(AccessRights right)
        {
            EnsureUserLoaded();
            if (!IsAuthenticated || !_cachedIsActive) return false;
            if (right == AccessRights.None) return true;
            return (Rights & (long)right) == (long)right;
        }

        public bool HasFeatureRight(FeatureRights right)
        {
            EnsureUserLoaded();
            if (!IsAuthenticated || !_cachedIsActive) return false;
            if (((_cachedRoleId ?? 0) & (int)UserRole.ADMIN) != 0) return true;
            if (!_cachedFeatureRights.HasValue)
            {
                var configured = _cachedGroupId.HasValue ? _context.UserGroups.AsNoTracking()
                    .Where(g => g.Id == _cachedGroupId.Value).Select(g => g.FeatureRightsId).FirstOrDefault() : null;
                _cachedFeatureRights = configured ?? FeatureRightsCatalog.Legacy(Rights);
            }
            return (_cachedFeatureRights.Value & (long)right) == (long)right;
        }

        public bool CanNominatePrefects
        {
            get
            {
                EnsureUserLoaded();
                if (!IsAuthenticated || !_cachedIsActive) return false;
                if (((_cachedRoleId ?? 0) & (int)UserRole.ADMIN) != 0) return true;
                if (_cachedCanNominatePrefects.HasValue) return _cachedCanNominatePrefects.Value;
                _cachedCanNominatePrefects = _cachedGroupId.HasValue && _context.UserGroups.AsNoTracking()
                    .Any(g => g.Id == _cachedGroupId && g.CanNominatePrefects);
                return _cachedCanNominatePrefects.Value;
            }
        }

        public async Task<Guid> GetUserIdAsync()
        {
            if (_cachedUserId.HasValue && _cachedIsActive)
                return _cachedUserId.Value;

            var userId = await EnsureUserExistsAsync();
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("No authenticated user found or auto-provisioning failed.");
            }

            return userId.Value;
        }

        // ------------------------------------------------------------
        // EnsureUserExistsAsync (auto-provisioning on first login)
        // ------------------------------------------------------------
        public async Task<Guid?> EnsureUserExistsAsync()
        {
            if (!IsAuthenticated)
                return null;

            if (_cachedUserId.HasValue) return _cachedIsActive ? _cachedUserId.Value : null;

            var email = Email;
            if (string.IsNullOrEmpty(email))
                return null;

            var localUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (localUser != null)
            {
                if (!localUser.IsActive) return null;
                localUser.IsEmailConfirmed = true;
                localUser.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _cachedUserId = localUser.Id;
                _cachedName = localUser.Name;
                _cachedIsActive = true;
                _cachedRoleId = localUser.RoleId;
                _cachedGroupId = localUser.GroupId;
                _hasLookedUpUser = true;
                return localUser.Id;
            }

            // --- New user: provision ---
            bool isBootstrapAdmin = !await _context.Users.AnyAsync()
                && string.Equals(_configuration?["Authentication:BootstrapAdminEmail"], email, StringComparison.OrdinalIgnoreCase);
            var role = isBootstrapAdmin ? UserRole.ADMIN : (UserRole)0;

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
                Name = ClaimName ?? email,
                Mobile = null,
                PasswordHash = null,
                IsActive = isBootstrapAdmin,
                ActivationDate = isBootstrapAdmin ? DateTime.UtcNow : null,
                CreationDate = DateTime.UtcNow,
                CreatorId = null,
                RoleId = (int)role,
                GroupId = null,
                IsEmailConfirmed = true,
                TwoFactorAuthEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                AuthRecoveryCodes = null,
                AuthenticatorKey = null,
                LockoutExpiryDate = null,
                LastLoginDate = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _cachedUserId = newUser.Id;
            _cachedName = newUser.Name;
            _cachedIsActive = newUser.IsActive;
            _cachedRoleId = newUser.RoleId;
            _cachedGroupId = newUser.GroupId;
            _hasLookedUpUser = true;

            return newUser.IsActive ? newUser.Id : null;
        }
    }
}
