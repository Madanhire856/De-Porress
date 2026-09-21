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

        // --- CLASSES ---
        public static int GetClassSequence(this SMSDbContext db, Class cls)
        {
            return db.Classes
                .AsNoTracking()
                .Where(c => c.CreationDate <= cls.CreationDate)
                .OrderBy(c => c.CreationDate)
                .ThenBy(c => c.Id)
                .Count();
        }

        public static void ApplyClassNumber(this SMSDbContext db, Class cls)
        {
            cls.ClassSequence = db.GetClassSequence(cls);
        }

        public static void ApplyClassNumbers(this SMSDbContext db, IEnumerable<Class> classes)
        {
            foreach (var c in classes)
                db.ApplyClassNumber(c);
        }

        // --- GRADES ---
        public static int GetGradeSequence(this SMSDbContext db, Grade grade)
        {
            return db.Grades
                .AsNoTracking()
                .Where(g => g.CreationDate <= grade.CreationDate)
                .OrderBy(g => g.CreationDate)
                .ThenBy(g => g.Id)
                .Count();
        }

        public static void ApplyGradeNumber(this SMSDbContext db, Grade grade)
        {
            grade.GradeSequence = db.GetGradeSequence(grade);
        }

        public static void ApplyGradeNumbers(this SMSDbContext db, IEnumerable<Grade> grades)
        {
            foreach (var g in grades)
                db.ApplyGradeNumber(g);
        }

        // --- HOUSES ---
        public static int GetHouseSequence(this SMSDbContext db, House house)
        {
            return db.Houses
                .AsNoTracking()
                .Where(h => h.CreationDate <= house.CreationDate)
                .OrderBy(h => h.CreationDate)
                .ThenBy(h => h.Id)
                .Count();
        }

        public static void ApplyHouseNumber(this SMSDbContext db, House house)
        {
            house.HouseSequence = db.GetHouseSequence(house);
        }

        public static void ApplyHouseNumbers(this SMSDbContext db, IEnumerable<House> houses)
        {
            foreach (var h in houses)
                db.ApplyHouseNumber(h);
        }

        // --- VILLAGES ---
        public static int GetVillageSequence(this SMSDbContext db, Village village)
        {
            return db.Villages
                .AsNoTracking()
                .Where(v => v.CreationDate <= village.CreationDate)
                .OrderBy(v => v.CreationDate)
                .ThenBy(v => v.Id)
                .Count();
        }

        public static void ApplyVillageNumber(this SMSDbContext db, Village village)
        {
            village.VillageSequence = db.GetVillageSequence(village);
        }

        public static void ApplyVillageNumbers(this SMSDbContext db, IEnumerable<Village> villages)
        {
            foreach (var v in villages)
                db.ApplyVillageNumber(v);
        }


        // --- TERMS ---
        public static int GetTermSequence(this SMSDbContext db, Term term)
        {
            return db.Terms
                .AsNoTracking()
                .Where(t => t.CreationDate <= term.CreationDate)
                .OrderBy(t => t.CreationDate)
                .ThenBy(t => t.Id)
                .Count();
        }

        public static void ApplyTermNumber(this SMSDbContext db, Term term)
        {
            term.TermSequence = db.GetTermSequence(term);
        }

        public static void ApplyTermNumbers(this SMSDbContext db, IEnumerable<Term> terms)
        {
            foreach (var t in terms)
                db.ApplyTermNumber(t);
        }
    }
}