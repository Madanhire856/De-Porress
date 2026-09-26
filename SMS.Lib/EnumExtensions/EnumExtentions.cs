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
            Gender.MALE => "Male",
            Gender.FEMALE => "Female",
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

        public static bool CanTeach(this StaffCategory c) => c switch
        {
            StaffCategory.GENERAL_TEACHER => true,
            StaffCategory.SENIOR_TEACHER => true,
            StaffCategory.STUDENT_TEACHER => true,
            StaffCategory.PRIEST => true,
            _ => false
        };

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

        // ==========================================================
        //  GRADE LEVEL
        // ==========================================================
        public static string ToDisplayName(this GradeLevel g) => g switch
        {
            GradeLevel.NONE => "—",
            GradeLevel.INFANT => "Infant",
            GradeLevel.JUNIOR => "Junior",
            GradeLevel.SENI => "Senior",
            _ => g.ToString()
        };

        public static IEnumerable<GradeLevel> AllGradeLevels() =>
            Enum.GetValues<GradeLevel>()
                .Where(g => g != GradeLevel.NONE)
                .OrderBy(g => (int)g);

        // ==========================================================
        //  ENROLMENT STATUS
        // ==========================================================
        public static string ToDisplayName(this EnrolmentStatus e) => e switch
        {
            EnrolmentStatus.ACTIVE => "Active",
            EnrolmentStatus.TRANSFERRED => "Transferred",
            EnrolmentStatus.GRADUATED => "Graduated",
            _ => e.ToString()
        };

        public static IEnumerable<EnrolmentStatus> AllEnrolmentStatuses() =>
            Enum.GetValues<EnrolmentStatus>()
                .OrderBy(e => (int)e);

        // ==========================================================
        //  STUDENT CATEGORY
        // ==========================================================
        public static string ToDisplayName(this StudentCategory c) => c switch
        {
            StudentCategory.GENERAL => "General",
            StudentCategory.ORPHAN => "Orphan",
            StudentCategory.DISABLED => "Disabled",
            _ => c.ToString()
        };

        public static IEnumerable<StudentCategory> AllStudentCategories() =>
            Enum.GetValues<StudentCategory>()
                .OrderBy(c => (int)c);

        // ==========================================================
        //  BAPTISM STATUS
        // ==========================================================
        public static string ToDisplayName(this BaptismStatus b) => b switch
        {
            BaptismStatus.BAPTISED => "Baptised",
            BaptismStatus.NOT_BAPTISED => "Not Baptised",
            _ => b.ToString()
        };

        public static IEnumerable<BaptismStatus> AllBaptismStatuses() =>
            Enum.GetValues<BaptismStatus>()
                .OrderBy(b => (int)b);

        // ==========================================================
        //  LEVY TYPE
        // ==========================================================
        public static string ToDisplayName(this LevyType l) => l switch
        {
            LevyType.TUITION => "Tuition",
            LevyType.DEVELOPMENT => "Development",
            LevyType.UNIFORM => "Uniform",
            LevyType.OTHER => "Other",
            _ => l.ToString()
        };

        public static IEnumerable<LevyType> AllLevyTypes() =>
            Enum.GetValues<LevyType>()
                .OrderBy(l => (int)l);

        // ==========================================================
        //  TEACHER HOUSE ROLE
        // ==========================================================
        public static string ToDisplayName(this TeacherHouseRole r) => r switch
        {
            TeacherHouseRole.MASTER => "House Master",
            TeacherHouseRole.ASSISTANT => "Assistant",
            _ => r.ToString()
        };

        public static IEnumerable<TeacherHouseRole> AllTeacherHouseRoles() =>
            Enum.GetValues<TeacherHouseRole>()
                .OrderBy(r => (int)r);

        // ==========================================================
        //  TEACHER SPORT ROLE
        // ==========================================================
        public static string ToDisplayName(this TeacherSportRole r) => r switch
        {
            TeacherSportRole.HEAD_COACH => "Head Coach",
            TeacherSportRole.ASSISTANT_COACH => "Assistant Coach",
            _ => r.ToString()
        };

        public static IEnumerable<TeacherSportRole> AllTeacherSportRoles() =>
            Enum.GetValues<TeacherSportRole>()
                .OrderBy(r => (int)r);

        // ==========================================================
        //  COMPETITION LEVEL
        // ==========================================================
        public static string ToDisplayName(this CompetitionLevel l) => l switch
        {
            CompetitionLevel.INTER_HOUSE => "Inter-House",
            CompetitionLevel.INTER_SCHOOLS => "Inter-Schools",
            CompetitionLevel.ZONAL => "Zonal",
            CompetitionLevel.CIRCUIT => "Circuit",
            CompetitionLevel.DISTRICT => "District",
            CompetitionLevel.PROVINCIAL => "Provincial",
            CompetitionLevel.NATIONAL => "National",
            CompetitionLevel.CATHOLIC_GAMES => "Catholic Games",
            _ => l.ToString()
        };

        public static IEnumerable<CompetitionLevel> AllCompetitionLevels() =>
            Enum.GetValues<CompetitionLevel>()
                .OrderBy(l => (int)l);

        // ==========================================================
        //  STUDENT LEDGER STATUS
        // ==========================================================
        public static string ToDisplayName(this StudentLedgerStatus s) => s switch
        {
            StudentLedgerStatus.SETTLED => "Settled",
            StudentLedgerStatus.OVER_DUE => "Over Due",
            StudentLedgerStatus.PARTIALLY_SETTLED => "Partially Settled",
            StudentLedgerStatus.SPONSORED => "Sponsored",
            _ => s.ToString()
        };

        public static IEnumerable<StudentLedgerStatus> AllStudentLedgerStatuses() =>
            Enum.GetValues<StudentLedgerStatus>()
                .OrderBy(s => (int)s);

        // ==========================================================
        //  PAYMENT METHOD
        // ==========================================================
        public static string ToDisplayName(this PaymentMethod m) => m switch
        {
            PaymentMethod.ECOCASH => "EcoCash",
            PaymentMethod.BANK_TRANSFER => "Bank Transfer",
            PaymentMethod.CASH => "Cash",
            PaymentMethod.OTHER => "Other",
            _ => m.ToString()
        };

        public static IEnumerable<PaymentMethod> AllPaymentMethods() =>
            Enum.GetValues<PaymentMethod>()
                .OrderBy(m => (int)m);

        /// <summary>
        /// Nullable overload — used when the stored value might be null.
        /// </summary>
        public static PaymentMethod? ParsePaymentMethod(int? stored)
        {
            if (!stored.HasValue) return null;
            return Enum.IsDefined(typeof(PaymentMethod), stored.Value)
                ? (PaymentMethod)stored.Value
                : null;
        }

        public static PaymentMethod? ParsePaymentMethod(int stored)
        {
            return Enum.IsDefined(typeof(PaymentMethod), stored)
                ? (PaymentMethod)stored
                : null;
        }

        // ==========================================================
        //  STAFF CATEGORY — HOUSE MASTER ELIGIBILITY
        // ==========================================================
        public static bool CanBeHouseMaster(this StaffCategory c) => c switch
        {
            StaffCategory.GENERAL_TEACHER => true,
            StaffCategory.SENIOR_TEACHER => true,
            _ => false
        };

        // ==========================================================
        //  PREFECT POST TITLE
        // ==========================================================
        public static string ToDisplayName(this PrefectPostTitle p) => p switch
        {
            PrefectPostTitle.HEAD_BOY => "Head Boy",
            PrefectPostTitle.HEAD_GIRL => "Head Girl",
            PrefectPostTitle.DEPUTY_HEAD_BOY => "Deputy Head Boy",
            PrefectPostTitle.DEPUTY_HEAD_GIRL => "Deputy Head Girl",
            PrefectPostTitle.SENIOR_BOY => "Senior Boy",
            PrefectPostTitle.SENIOR_GIRL => "Senior Girl",
            PrefectPostTitle.PREFECT => "Prefect",
            PrefectPostTitle.SPORTS_PREFECT => "Sports Prefect",
            PrefectPostTitle.FLAG_BEARER => "Flag Bearer",
            _ => p.ToString()
        };

        public static IEnumerable<PrefectPostTitle> AllPrefectPostTitles() =>
            Enum.GetValues<PrefectPostTitle>()
                .OrderBy(p => (int)p);

        // ==========================================================
        //  PREFECT STATUS
        // ==========================================================
        public static string ToDisplayName(this PrefectStatus s) => s switch
        {
            PrefectStatus.ACTIVE => "Active",
            PrefectStatus.INACTIVE => "Inactive",
            PrefectStatus.DEMOTED => "Demoted",
            PrefectStatus.COMPLETED => "Completed",
            _ => s.ToString()
        };

        public static IEnumerable<PrefectStatus> AllPrefectStatuses() =>
            Enum.GetValues<PrefectStatus>()
                .OrderBy(s => (int)s);

        // ==========================================================
        //  PREFECT APPOINTMENT STATUS
        // ==========================================================
        public static string ToDisplayName(this PrefectAppointmentStatus s) => s switch
        {
            PrefectAppointmentStatus.NOMINATED => "Nominated",
            PrefectAppointmentStatus.APPOINTED => "Appointed",
            PrefectAppointmentStatus.VETTING_IN_PROGRESS => "Vetting In Progress",
            PrefectAppointmentStatus.COMPLETED => "Completed",
            PrefectAppointmentStatus.DEMOTED => "Demoted",
            _ => s.ToString()
        };

        public static IEnumerable<PrefectAppointmentStatus> AllPrefectAppointmentStatuses() =>
            Enum.GetValues<PrefectAppointmentStatus>()
                .OrderBy(s => (int)s);

        // ==========================================================
        //  PREFECT — HELPERS
        // ==========================================================
        public static bool IsActive(this PrefectStatus s) =>
            s == PrefectStatus.ACTIVE;

        public static bool IsLeadershipRole(this PrefectPostTitle p) => p switch
        {
            PrefectPostTitle.HEAD_BOY => true,
            PrefectPostTitle.HEAD_GIRL => true,
            PrefectPostTitle.DEPUTY_HEAD_BOY => true,
            PrefectPostTitle.DEPUTY_HEAD_GIRL => true,
            PrefectPostTitle.SENIOR_BOY => true,
            PrefectPostTitle.SENIOR_GIRL => true,
            _ => false
        };
    }
}