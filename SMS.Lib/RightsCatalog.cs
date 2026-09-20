using System;
using System.Collections.Generic;

namespace SMS.Lib
{
    public class RightItem
    {
        public AccessRights Right { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class RightCategory
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-circle";
        public List<RightItem> Rights { get; set; } = new();
    }

    public static class RightsCatalog
    {
        public static List<RightCategory> GetCategories()
        {
            return new List<RightCategory>
            {
                new RightCategory
                {
                    Title = "Dashboard",
                    Icon = "fa-tachometer-alt",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewDashboard, DisplayName = "View Dashboard" }
                    }
                },
                new RightCategory
                {
                    Title = "Configuration",
                    Icon = "fa-cogs",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ConfigClasses,  DisplayName = "Configure Classes" },
                        new() { Right = AccessRights.ConfigStaff,    DisplayName = "Configure Staff" },
                        new() { Right = AccessRights.ConfigFees,     DisplayName = "Configure Fees" },
                        new() { Right = AccessRights.ConfigMass,     DisplayName = "Configure Mass & Liturgy" },
                        new() { Right = AccessRights.ConfigSports,   DisplayName = "Configure Sports" },
                        new() { Right = AccessRights.ConfigSyllabus, DisplayName = "Configure Syllabus Repository" },
                        new() { Right = AccessRights.ConfigSystem,   DisplayName = "System Configuration" }
                    }
                },
                new RightCategory
                {
                    Title = "Academics & Learners",
                    Icon = "fa-user-graduate",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewLearners,       DisplayName = "View Learners" },
                        new() { Right = AccessRights.EditLearners,       DisplayName = "Edit Learners" },
                        new() { Right = AccessRights.MarkAttendance,     DisplayName = "Mark Attendance" },
                        new() { Right = AccessRights.GenerateReportCards,DisplayName = "Generate Report Cards" },
                        new() { Right = AccessRights.ViewAllClasses,     DisplayName = "View All Classes" },
                        new() { Right = AccessRights.ViewOwnClass,       DisplayName = "View Own Class Only" },
                        new() { Right = AccessRights.ProcessPromotions,  DisplayName = "Process Promotions" },
                        new() { Right = AccessRights.ProcessTransfers,   DisplayName = "Process Transfers" }
                    }
                },
                new RightCategory
                {
                    Title = "Fees & Finance",
                    Icon = "fa-hand-holding-usd",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewFeeBalances, DisplayName = "View Fee Balances" },
                        new() { Right = AccessRights.RecordPayments,  DisplayName = "Record Payments" },
                        new() { Right = AccessRights.GenerateInvoices,DisplayName = "Generate Invoices" },
                        new() { Right = AccessRights.WaiveFees,       DisplayName = "Waive Fees" },
                        new() { Right = AccessRights.ViewFeeReports,  DisplayName = "View Fee Reports" }
                    }
                },
                new RightCategory
                {
                    Title = "Mass & Liturgy",
                    Icon = "fa-church",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewMassSchedule,          DisplayName = "View Mass Schedule" },
                        new() { Right = AccessRights.CreateMassRota,            DisplayName = "Create Mass Rota" },
                        new() { Right = AccessRights.EditMassRota,              DisplayName = "Edit Mass Rota" },
                        new() { Right = AccessRights.DeleteMassRota,            DisplayName = "Delete Mass Rota" },
                        new() { Right = AccessRights.RecordSacraments,          DisplayName = "Record Sacraments" },
                        new() { Right = AccessRights.CreateLiturgicalCalendar,  DisplayName = "Create Liturgical Calendar" },
                        new() { Right = AccessRights.EditLiturgicalCalendar,    DisplayName = "Edit Liturgical Calendar" },
                        new() { Right = AccessRights.DeleteLiturgicalCalendar,  DisplayName = "Delete Liturgical Calendar" }
                    }
                },
                new RightCategory
                {
                    Title = "Sports & Co-Curricular",
                    Icon = "fa-futbol",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSports,           DisplayName = "View Sports" },
                        new() { Right = AccessRights.CreateSportsEvents,   DisplayName = "Create Sports Events" },
                        new() { Right = AccessRights.EditSportsEvents,     DisplayName = "Edit Sports Events" },
                        new() { Right = AccessRights.DeleteSportsEvents,   DisplayName = "Delete Sports Events" },
                        new() { Right = AccessRights.RecordSportsResults,  DisplayName = "Record Sports Results" },
                        new() { Right = AccessRights.CreateEquipment,      DisplayName = "Create Equipment" },
                        new() { Right = AccessRights.EditEquipment,        DisplayName = "Edit Equipment" },
                        new() { Right = AccessRights.DeleteEquipment,      DisplayName = "Delete Equipment" },
                        new() { Right = AccessRights.CheckoutEquipment,    DisplayName = "Checkout Equipment" }
                    }
                },
                new RightCategory
                {
                    Title = "Syllabus Repository",
                    Icon = "fa-book",
                    Rights = new()
                    {
                        new() { Right = AccessRights.UploadSyllabus, DisplayName = "Upload Syllabus" },
                        new() { Right = AccessRights.ViewSyllabus,   DisplayName = "View Syllabus" }
                    }
                },
                new RightCategory
                {
                    Title = "AI Features",
                    Icon = "fa-robot",
                    Rights = new()
                    {
                        new() { Right = AccessRights.UseLessonNotes, DisplayName = "Use AI Lesson Notes" }
                    }
                },
                new RightCategory
                {
                    Title = "User Management",
                    Icon = "fa-users-cog",
                    Rights = new()
                    {
                        new() { Right = AccessRights.CreateUsers,    DisplayName = "Create Users" },
                        new() { Right = AccessRights.EditUsers,      DisplayName = "Edit Users" },
                        new() { Right = AccessRights.DeleteUsers,    DisplayName = "Delete Users" },
                        new() { Right = AccessRights.ViewUsers,      DisplayName = "View Users" },
                        new() { Right = AccessRights.ResetPasswords, DisplayName = "Reset Passwords" }
                    }
                },
                new RightCategory
                {
                    Title = "Welfare & Pastoral",
                    Icon = "fa-heart",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewWelfareData, DisplayName = "View Welfare Data" }
                    }
                },
                new RightCategory
                {
                    Title = "Reports",
                    Icon = "fa-chart-bar",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSchoolReports, DisplayName = "View School Reports" },
                        new() { Right = AccessRights.ExportReports,     DisplayName = "Export Reports" }
                    }
                },
                new RightCategory
                {
                    Title = "Houses",
                    Icon = "fa-shield-alt",
                    Rights = new()
                    {
                        new() { Right = AccessRights.CreateHouses, DisplayName = "Create Houses" },
                        new() { Right = AccessRights.EditHouses,   DisplayName = "Edit Houses" },
                        new() { Right = AccessRights.DeleteHouses, DisplayName = "Delete Houses" }
                    }
                },
                new RightCategory
                {
                    Title = "Sponsors",
                    Icon = "fa-handshake",
                    Rights = new()
                    {
                        new() { Right = AccessRights.CreateSponsors, DisplayName = "Create Sponsors" },
                        new() { Right = AccessRights.EditSponsors,   DisplayName = "Edit Sponsors" },
                        new() { Right = AccessRights.DeleteSponsors, DisplayName = "Delete Sponsors" }
                    }
                },
                new RightCategory
                {
                    Title = "System Administration",
                    Icon = "fa-server",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewAuditLogs,    DisplayName = "View Audit Logs" },
                        new() { Right = AccessRights.CreateBackups,    DisplayName = "Create Backups" },
                        new() { Right = AccessRights.RestoreBackups,   DisplayName = "Restore Backups" },
                        new() { Right = AccessRights.DeleteBackups,    DisplayName = "Delete Backups" },
                        new() { Right = AccessRights.TriggerSync,      DisplayName = "Trigger Sync" },
                        new() { Right = AccessRights.ConfigureSync,    DisplayName = "Configure Sync" },
                        new() { Right = AccessRights.SystemSettings,   DisplayName = "System Settings" }
                    }
                }
            };
        }
    }
}