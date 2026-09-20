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

        private Guid? _cachedUserId;
        private bool _hasLookedUpUser;

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


        public Guid? UserId
        {
            get
            {
                if (_hasLookedUpUser) return _cachedUserId;

                var email = Email;
                if (string.IsNullOrEmpty(email))
                {
                    _hasLookedUpUser = true;
                    return null;
                }

                var localUser = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Email == email);

                _cachedUserId = localUser?.Id;
                _hasLookedUpUser = true;
                return _cachedUserId;
            }
        }

        // ------------------------------------------------------------
        // GetUserIdAsync (throws if not found, no auto-create)
        // ------------------------------------------------------------
        public async Task<Guid> GetUserIdAsync()
        {
            if (_hasLookedUpUser && _cachedUserId.HasValue)
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
            _hasLookedUpUser = true;
            return localUser.Id;
        }

     
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
                _hasLookedUpUser = true;
                return localUser.Id;
            }

           
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
                PasswordHash = null,          // Azure AD handles authentication
                IsActive = true,
                ActivationDate = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatorId = null,             // System-created
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
            _hasLookedUpUser = true;

            return newUser.Id;
        }
    }
}