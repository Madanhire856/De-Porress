using SMS.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Academics.Pages.Prefects.ViewModels
{
    public class PrefectVM
    {
        public Guid Id { get; set; }

        // ============================================================
        //  STUDENT
        // ============================================================
        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public Guid StudentId { get; set; }

        public string? StudentName { get; set; }
        public string? StudentSurname { get; set; }
        public string? StudentNumber { get; set; }
        public string? StudentClass { get; set; }
        public string? StudentPhotoUrl { get; set; }
        public int? StudentGenderId { get; set; }
        public DateTime? StudentDob { get; set; }

        // ============================================================
        //  APPOINTMENT
        // ============================================================
        [Required(ErrorMessage = "Post title is required.")]
        [Display(Name = "Post Title")]
        public int PostTitleId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public int StatusId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}",
                       ApplyFormatInEditMode = true)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        // NOTE: No default value — must be explicitly set by the user.
        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}",
                       ApplyFormatInEditMode = true)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        // ============================================================
        //  NOMINATOR
        // ============================================================
        [Display(Name = "Nominator")]
        public Guid? NominatorId { get; set; }

        public string? NominatorName { get; set; }
        public DateTime? CreationDate { get; set; }

        // ============================================================
        //  DISPLAY-ONLY HELPERS
        // ============================================================
        public bool IsEdit => Id != Guid.Empty;

        public string PostTitleName =>
            Enum.IsDefined(typeof(PrefectPostTitle), PostTitleId)
                ? ((PrefectPostTitle)PostTitleId).ToDisplayName()
                : "—";

        public string StatusName =>
            Enum.IsDefined(typeof(PrefectStatus), StatusId)
                ? ((PrefectStatus)StatusId).ToDisplayName()
                : "—";

        public string StatusCssClass => (PrefectStatus)StatusId switch
        {
            PrefectStatus.ACTIVE => "status-active",
            PrefectStatus.INACTIVE => "status-inactive",
            PrefectStatus.DEMOTED => "status-danger",
            PrefectStatus.COMPLETED => "status-completed",
            _ => "status-inactive"
        };

        public string StudentFullName =>
            $"{StudentName} {StudentSurname}".Trim();

        public string StudentInitials
        {
            get
            {
                var n = (StudentName ?? "").Trim();
                var s = (StudentSurname ?? "").Trim();
                var i1 = n.Length > 0 ? n[0].ToString() : "";
                var i2 = s.Length > 0 ? s[0].ToString() : "";
                return (i1 + i2).ToUpper();
            }
        }

        public int? StudentAge
        {
            get
            {
                if (!StudentDob.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - StudentDob.Value.Year;
                if (StudentDob.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public string StudentGenderName =>
            StudentGenderId.HasValue && Enum.IsDefined(typeof(Gender), StudentGenderId.Value)
                ? ((Gender)StudentGenderId.Value).ToDisplayName()
                : "—";

        public int DurationInDays =>
            !EndDate.HasValue || StartDate == default
                ? 0
                : Math.Max(0, (EndDate.Value.Date - StartDate.Date).Days);

        public string DurationDisplay
        {
            get
            {
                var totalDays = DurationInDays;
                if (totalDays == 0) return "—";

                var years = totalDays / 365;
                var months = (totalDays % 365) / 30;
                var days = (totalDays % 365) % 30;

                var parts = new List<string>();
                if (years > 0) parts.Add($"{years} yr{(years == 1 ? "" : "s")}");
                if (months > 0) parts.Add($"{months} mo");
                if (days > 0) parts.Add($"{days} d");

                return parts.Count > 0 ? string.Join(" ", parts) : $"{totalDays} d";
            }
        }
    }
}