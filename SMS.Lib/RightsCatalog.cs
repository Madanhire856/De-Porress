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
                    Title = "System",
                    Icon = "fa-server",
                    Rights = new()
                    {
                        new() { Right = AccessRights.SystemSettings,   DisplayName = "System Settings" },
                        new() { Right = AccessRights.ViewAuditLogs,    DisplayName = "View Audit Logs" },
                        new() { Right = AccessRights.CreateBackups,    DisplayName = "Create Backups" },
                        new() { Right = AccessRights.RestoreBackups,   DisplayName = "Restore Backups" },
                        new() { Right = AccessRights.DeleteBackups,    DisplayName = "Delete Backups" },
                        new() { Right = AccessRights.TriggerSync,      DisplayName = "Trigger Sync" },
                        new() { Right = AccessRights.ConfigureSync,    DisplayName = "Configure Sync" }
                    }
                },
                new RightCategory
                {
                    Title = "Users",
                    Icon = "fa-users",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewUsers,   DisplayName = "View Users" },
                        new() { Right = AccessRights.CreateUsers, DisplayName = "Create Users" },
                        new() { Right = AccessRights.EditUsers,   DisplayName = "Edit Users" }
                    }
                },
                new RightCategory
                {
                    Title = "User Groups",
                    Icon = "fa-users-cog",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewUserGroups,   DisplayName = "View User Groups" },
                        new() { Right = AccessRights.CreateUserGroups, DisplayName = "Create User Groups" },
                        new() { Right = AccessRights.EditUserGroups,   DisplayName = "Edit User Groups" }
                    }
                },
                new RightCategory
                {
                    Title = "Staff",
                    Icon = "fa-user-tie",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewStaff,   DisplayName = "View Staff" },
                        new() { Right = AccessRights.CreateStaff, DisplayName = "Create Staff" },
                        new() { Right = AccessRights.EditStaff,   DisplayName = "Edit Staff" }
                    }
                },
                new RightCategory
                {
                    Title = "Grades",
                    Icon = "fa-layer-group",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewGrades,   DisplayName = "View Grades" },
                        new() { Right = AccessRights.CreateGrades, DisplayName = "Create Grades" },
                        new() { Right = AccessRights.EditGrades,   DisplayName = "Edit Grades" }
                    }
                },
                new RightCategory
                {
                    Title = "Classes",
                    Icon = "fa-chalkboard",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewClasses,   DisplayName = "View Classes" },
                        new() { Right = AccessRights.CreateClasses, DisplayName = "Create Classes" },
                        new() { Right = AccessRights.EditClasses,   DisplayName = "Edit Classes" }
                    }
                },
                new RightCategory
                {
                    Title = "Subjects",
                    Icon = "fa-book",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSubjects,   DisplayName = "View Subjects" },
                        new() { Right = AccessRights.CreateSubjects, DisplayName = "Create Subjects" },
                        new() { Right = AccessRights.EditSubjects,   DisplayName = "Edit Subjects" }
                    }
                },
                new RightCategory
                    {
                        Title = "Terms",
                        Icon = "fa-calendar",
                        Rights = new()
                        {
                            new() { Right = AccessRights.ViewTerms,   DisplayName = "View Terms" },
                            new() { Right = AccessRights.CreateTerms, DisplayName = "Create Terms" },
                            new() { Right = AccessRights.EditTerms,   DisplayName = "Edit Terms" }
                        }
                    },

                new RightCategory
                    {
                        Title = "Currencies",
                        Icon = "fa-coins",
                        Rights = new()
                        {
                            new() { Right = AccessRights.ViewCurrencies,   DisplayName = "View Currencies" },
                            new() { Right = AccessRights.CreateCurrencies, DisplayName = "Create Currencies" },
                            new() { Right = AccessRights.EditCurrencies,   DisplayName = "Edit Currencies" }
                        }
                    },
                new RightCategory
                {
                    Title = "Timetable",
                    Icon = "fa-calendar-alt",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewTimetable,   DisplayName = "View Timetable" },
                        new() { Right = AccessRights.CreateTimetable, DisplayName = "Create Timetable" },
                        new() { Right = AccessRights.EditTimetable,   DisplayName = "Edit Timetable" }
                    }
                },
                new RightCategory
                {
                    Title = "Learners",
                    Icon = "fa-user-graduate",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewLearners,        DisplayName = "View Learners" },
                        new() { Right = AccessRights.CreateLearners,      DisplayName = "Create Learners" },
                        new() { Right = AccessRights.EditLearners,        DisplayName = "Edit Learners" },
                        new() { Right = AccessRights.MarkAttendance,      DisplayName = "Mark Attendance" },
                        new() { Right = AccessRights.GenerateReportCards, DisplayName = "Generate Report Cards" }
                    }
                },
                new RightCategory
                {
                    Title = "Houses",
                    Icon = "fa-shield-alt",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewHouses,   DisplayName = "View Houses" },
                        new() { Right = AccessRights.CreateHouses, DisplayName = "Create Houses" },
                        new() { Right = AccessRights.EditHouses,   DisplayName = "Edit Houses" }
                    }
                },
                new RightCategory
                {
                    Title = "Villages",
                    Icon = "fa-map-marker-alt",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewVillages,   DisplayName = "View Villages" },
                        new() { Right = AccessRights.CreateVillages, DisplayName = "Create Villages" },
                        new() { Right = AccessRights.EditVillages,   DisplayName = "Edit Villages" }
                    }
                },
                new RightCategory
                {
                    Title = "Fees & Finance",
                    Icon = "fa-hand-holding-usd",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewFeeBalances,      DisplayName = "View Fee Balances" },
                        new() { Right = AccessRights.RecordPayments,       DisplayName = "Record Payments" },
                        new() { Right = AccessRights.GenerateInvoices,     DisplayName = "Generate Invoices" },
                        new() { Right = AccessRights.WaiveFees,            DisplayName = "Waive Fees" },
                        new() { Right = AccessRights.ViewFeeReports,       DisplayName = "View Fee Reports" },
                        new() { Right = AccessRights.ViewAllBursarsCashUp, DisplayName = "View All Bursars' Cash-Up" }
                    }
                },
                new RightCategory
                {
                    Title = "Sponsors",
                    Icon = "fa-handshake",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSponsors,   DisplayName = "View Sponsors" },
                        new() { Right = AccessRights.CreateSponsors, DisplayName = "Create Sponsors" },
                        new() { Right = AccessRights.EditSponsors,   DisplayName = "Edit Sponsors" }
                    }
                },
                new RightCategory
                {
                    Title = "Sponsorships",
                    Icon = "fa-link",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSponsorships, DisplayName = "View Sponsorships" }
                    }
                },

                new RightCategory
                {
                    Title = "Sports",
                    Icon = "fa-futbol",
                    Rights = new()
                    {
                        new() { Right = AccessRights.ViewSports,          DisplayName = "View Sports" },
                        new() { Right = AccessRights.CreateSports,        DisplayName = "Create Sports" },
                        new() { Right = AccessRights.EditSports,          DisplayName = "Edit Sports" },
                        new() { Right = AccessRights.AssignSportsCoaches, DisplayName = "Assign Sports Coaches" }
                    }
                },
            };
        }
    }
}