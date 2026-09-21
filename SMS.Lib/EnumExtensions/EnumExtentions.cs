using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Lib
{
    public static class EnumExtensions
    {
        // ==========================================================
        //  GENDER
        // ==========================================================
        public static string ToDisplayName(this Gender g) => g switch
        {
            Gender.Male => "Male",
            Gender.Female => "Female",
            _ => g.ToString()
        };

        public static IEnumerable<Gender> AllGenders() =>
            Enum.GetValues<Gender>().OrderBy(g => g.ToDisplayName());

        // ==========================================================
        //  MARITAL STATUS
        // ==========================================================
        public static string ToDisplayName(this MaritalStatus m) => m switch
        {
            MaritalStatus.SINGLE => "Single",
            MaritalStatus.MARRIED => "Married",
            MaritalStatus.DIVORCED => "Divorced",
            _ => m.ToString()
        };

        public static IEnumerable<MaritalStatus> AllMaritalStatuses() =>
            Enum.GetValues<MaritalStatus>().OrderBy(m => (int)m);

        // ==========================================================
        //  STAFF CATEGORY
        // ==========================================================
        public static string ToDisplayName(this StaffCategory c) => c switch
        {
            StaffCategory.None => "—",
            StaffCategory.GENERAL_TEACHER => "General Teacher",
            StaffCategory.SENIOR_TEACHER => "Senior Teacher",
            StaffCategory.STUDENT_TEACHER => "Student Teacher",
            StaffCategory.PRIEST => "Priest",
            StaffCategory.ANCILLARY => "Ancillary",
            _ => c.ToString()
        };

        public static IEnumerable<StaffCategory> AllStaffCategories() =>
            Enum.GetValues<StaffCategory>()
                .Where(c => c != StaffCategory.None)
                .OrderBy(c => (int)c);

        // ==========================================================
        //  STAFF STATUS
        // ==========================================================
        public static string ToDisplayName(this StaffStatus s) => s switch
        {
            StaffStatus.ACTIVE => "Active",
            StaffStatus.ON_LEAVE => "On Leave",
            _ => s.ToString()
        };

        public static IEnumerable<StaffStatus> AllStaffStatuses() =>
            Enum.GetValues<StaffStatus>().OrderBy(s => (int)s);

        // ==========================================================
        //  QUALIFICATION
        // ==========================================================
        public static string ToDisplayName(this Qualification q) => q switch
        {
            Qualification.NONE => "—",
            Qualification.DIPLOMA => "Diploma",
            Qualification.DEGREE => "Degree",
            Qualification.MASTERS => "Masters",
            Qualification.PHD => "PhD",
            _ => q.ToString()
        };

        public static IEnumerable<Qualification> AllQualifications() =>
            Enum.GetValues<Qualification>()
                .Where(q => q != Qualification.NONE)
                .OrderBy(q => (int)q);

        // ==========================================================
        //  SUBJECT CATEGORY
        // ==========================================================
        public static string ToDisplayName(this SubjectCategory c) => c switch
        {
            SubjectCategory.NONE => "—",
            SubjectCategory.PRACTICAL => "Practical",
            SubjectCategory.CORE => "Core",
            SubjectCategory.CO_RECURRICULAR => "Co-Curricular",
            SubjectCategory.FAITH_LIFE => "Faith & Life",
            _ => c.ToString()
        };

        public static IEnumerable<SubjectCategory> AllSubjectCategories() =>
            Enum.GetValues<SubjectCategory>()
                .Where(c => c != SubjectCategory.NONE)
                .OrderBy(c => (int)c);
    }
}