using System;
using System.Threading.Tasks;

namespace SMS.Lib
{
    public interface ICurrentUserService
    {
        // --- Identity-provider claims ---
        string? Email { get; }
        string? Name { get; }
        string? IdentitySubject { get; }
        bool IsAuthenticated { get; }

        // --- Local Database User ---
        Guid? UserId { get; }
        int? RoleId { get; }
        Guid? GroupId { get; }

        Task<Guid> GetUserIdAsync();
        Task<Guid?> EnsureUserExistsAsync();

        // --- Aggregated Rights ---
        long Rights { get; }
        bool CanNominatePrefects { get; }
        bool HasRight(AccessRights right);
    }
}
