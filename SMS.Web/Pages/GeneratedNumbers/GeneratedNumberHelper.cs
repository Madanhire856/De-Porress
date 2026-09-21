using Microsoft.EntityFrameworkCore;
using SMS.Data;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Pages.GeneratedNumbers
{
    public static class GeneratedNumberHelper
    {
        // --- USER GROUPS ---

        public static int GetUserGroupSequence(
            this SMSDbContext db,
            UserGroup userGroup)
        {
            return db.UserGroups
                .AsNoTracking()
                .Where(g => g.CreationDate <= userGroup.CreationDate)
                .OrderBy(g => g.CreationDate)
                .ThenBy(g => g.Id)
                .Count();
        }

        public static void ApplyUserGroupNumber(
            this SMSDbContext db,
            UserGroup userGroup)
        {
            userGroup.UserGroupSequence = db.GetUserGroupSequence(userGroup);
        }

        public static void ApplyUserGroupNumbers(
            this SMSDbContext db,
            IEnumerable<UserGroup> userGroups)
        {
            foreach (var group in userGroups)
            {
                db.ApplyUserGroupNumber(group);
            }
        }

        // --- USERS ---
        public static int GetUserSequence(this SMSDbContext db, User user)
        {
            return db.Users
                .AsNoTracking()
                .Where(u => u.CreationDate <= user.CreationDate)
                .OrderBy(u => u.CreationDate)
                .ThenBy(u => u.Id)
                .Count();
        }

        public static void ApplyUserNumber(this SMSDbContext db, User user)
        {
            user.UserSequence = db.GetUserSequence(user);
        }

        public static void ApplyUserNumbers(this SMSDbContext db, IEnumerable<User> users)
        {
            foreach (var user in users)
                db.ApplyUserNumber(user);
        }

        // --- SUBJECTS ---
        public static int GetSubjectSequence(this SMSDbContext db, Subject subject)
        {
            return db.Subjects
                .AsNoTracking()
                .Where(s => s.CreationDate <= subject.CreationDate)
                .OrderBy(s => s.CreationDate)
                .ThenBy(s => s.Id)
                .Count();
        }

        public static void ApplySubjectNumber(this SMSDbContext db, Subject subject)
        {
            subject.SubjectSequence = db.GetSubjectSequence(subject);
        }

        public static void ApplySubjectNumbers(this SMSDbContext db, IEnumerable<Subject> subjects)
        {
            foreach (var subject in subjects)
                db.ApplySubjectNumber(subject);
        }
    }
}