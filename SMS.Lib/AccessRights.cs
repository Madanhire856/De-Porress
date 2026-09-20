using System;

namespace SMS.Lib
{
    [Flags]
    public enum AccessRights : long
    {
        None = 0,
        ViewDashboard = 1L << 0,

        // --- Configuration (Structural Setup) ---
        ConfigClasses = 1L << 1,
        ConfigStaff = 1L << 2,
        ConfigFees = 1L << 3,
        ConfigMass = 1L << 4,
        ConfigSports = 1L << 5,
        ConfigSyllabus = 1L << 6,
        ConfigSystem = 1L << 7,

        // --- Academics (Learners & Classes) ---
        ViewLearners = 1L << 8,
        EditLearners = 1L << 9,
        MarkAttendance = 1L << 10,
        GenerateReportCards = 1L << 11,
        ViewAllClasses = 1L << 12,
        ViewOwnClass = 1L << 13,
        ProcessPromotions = 1L << 14,
        ProcessTransfers = 1L << 15,

        // --- Fees ---
        ViewFeeBalances = 1L << 16,
        RecordPayments = 1L << 17,
        GenerateInvoices = 1L << 18,
        WaiveFees = 1L << 19,
        ViewFeeReports = 1L << 20,

        // --- Mass & Liturgy ---
        ViewMassSchedule = 1L << 21,
        CreateMassRota = 1L << 22,
        EditMassRota = 1L << 23,
        DeleteMassRota = 1L << 24,
        RecordSacraments = 1L << 25,
        CreateLiturgicalCalendar = 1L << 26,
        EditLiturgicalCalendar = 1L << 27,
        DeleteLiturgicalCalendar = 1L << 28,

        // --- Sports & Co-Curricular ---
        ViewSports = 1L << 29,
        CreateSportsEvents = 1L << 30,
        EditSportsEvents = 1L << 31,
        DeleteSportsEvents = 1L << 32,
        RecordSportsResults = 1L << 33,
        CreateEquipment = 1L << 34,
        EditEquipment = 1L << 35,
        DeleteEquipment = 1L << 36,
        CheckoutEquipment = 1L << 37,

        // --- Syllabus Repository ---
        UploadSyllabus = 1L << 38,
        ViewSyllabus = 1L << 39,

        // --- AI Features (Lesson Notes only) ---
        UseLessonNotes = 1L << 40,

        // --- User Management ---
        CreateUsers = 1L << 41,
        EditUsers = 1L << 42,
        DeleteUsers = 1L << 43,
        ViewUsers = 1L << 44,
        ResetPasswords = 1L << 45,

        // --- System Administration ---
        ViewAuditLogs = 1L << 46,
        CreateBackups = 1L << 47,
        RestoreBackups = 1L << 48,
        DeleteBackups = 1L << 49,
        TriggerSync = 1L << 50,
        ConfigureSync = 1L << 51,
        SystemSettings = 1L << 52,

        // --- Welfare / Pastoral ---
        ViewWelfareData = 1L << 53,

        // --- Reporting ---
        ViewSchoolReports = 1L << 54,
        ExportReports = 1L << 55,

        // --- Houses ---
        CreateHouses = 1L << 56,
        EditHouses = 1L << 57,
        DeleteHouses = 1L << 58,

        // --- Sponsors ---
        CreateSponsors = 1L << 59,
        EditSponsors = 1L << 60,
        DeleteSponsors = 1L << 61,
    }
}