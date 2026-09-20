using System;
using System.Threading.Tasks;

namespace SMS.Lib
{
    public interface ICurrentUserService
    {
        // --- Azure AD Claims ---
        string? Email { get; }
        string? Name { get; }
        string? AzureId { get; }
        bool IsAuthenticated { get; }

        // --- Local Database User ---
        Guid? UserId { get; }
        Task<Guid> GetUserIdAsync();
        Task<Guid?> EnsureUserExistsAsync();
    }
}