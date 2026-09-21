using System;
using System.Collections.Generic;
using System.Linq;
using SMS.Data;

namespace SMS.Lib
{
    public static class UserRoleExtensions
    {

        public static string ToDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.CLASS_TEACHER => "Class Teacher",
                UserRole.HEAD => "Head",
                UserRole.DEPUTY_HEAD => "Deputy Head",
                UserRole.BURSER => "Burser",
                UserRole.PARENT => "Parent",
                UserRole.STAFF => "Staff",
                UserRole.ADMIN => "Administrator",
                _ => role.ToString()
            };
        }

        /// <summary>Ordered list of all selectable roles (for dropdowns).</summary>
        public static IEnumerable<UserRole> AllRoles()
        {
            return Enum.GetValues<UserRole>()
                       .OrderBy(r => r.ToDisplayName());
        }

        public static string GetRoleDisplayName(this User user)
        {
            if (user == null) return "—";
            return ((UserRole)user.RoleId).ToDisplayName();
        }

        public static bool HasRole(this User user, UserRole role)
        {
            if (user == null) return false;
            return ((UserRole)user.RoleId).HasFlag(role);
        }
        public static bool IsAdmin(this User user)
        {
            return user.HasRole(UserRole.ADMIN);
        }
    }
}