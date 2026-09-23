using SMS.Lib;
using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Academics.Pages.Students.ViewModels
{
    public class StudentVM
    {
        public Guid Id { get; set; }

        // IDENTITY
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "First Name")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters.")]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = "";

        [Required(ErrorMessage = "Gender is required.")]
        [Display(Name = "Gender")]
        public int GenderId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? Dob { get; set; }

        [StringLength(50)]
        [Display(Name = "Birth Entry Number")]
        public string? BirthEntryNumber { get; set; }

        [StringLength(300)]
        [Display(Name = "Photo URL")]
        public string? PhotoUrl { get; set; }

        // ENROLMENT
        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Enrolment status is required.")]
        [Display(Name = "Enrolment Status")]
        public int EnrolmentStatusId { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Enrolment Date")]
        public DateTime? EnrolmentDate { get; set; }

        // FAITH
        [Display(Name = "Is Catholic")]
        public bool IsCatholic { get; set; }

        [Display(Name = "Baptism Status")]
        public int? BaptismStatusId { get; set; }

        // LOCATION
        [Display(Name = "Village")]
        public Guid? VillageId { get; set; }

        // HEALTH
        [Display(Name = "Allergies")]
        public string? AllergieNotesJson { get; set; }

        [Display(Name = "Disabilities")]
        public string? DisabilityNotesJson { get; set; }

        // DISPLAY
        public string FullName => $"{Name} {Surname}".Trim();
        public string StudentNumber { get; set; } = "";

        public string Initials
        {
            get
            {
                var n = (Name ?? "").Trim();
                var s = (Surname ?? "").Trim();
                var i1 = n.Length > 0 ? n[0].ToString() : "";
                var i2 = s.Length > 0 ? s[0].ToString() : "";
                return (i1 + i2).ToUpper();
            }
        }

        public int? Age
        {
            get
            {
                if (!Dob.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - Dob.Value.Year;
                if (Dob.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public string GenderName =>
            Enum.IsDefined(typeof(Gender), GenderId)
                ? ((Gender)GenderId).ToDisplayName()
                : "—";

        public string CategoryName =>
            Enum.IsDefined(typeof(StudentCategory), CategoryId)
                ? ((StudentCategory)CategoryId).ToDisplayName()
                : "—";

        public string EnrolmentStatusName =>
            Enum.IsDefined(typeof(EnrolmentStatus), EnrolmentStatusId)
                ? ((EnrolmentStatus)EnrolmentStatusId).ToDisplayName()
                : "—";

        public string? BaptismStatusName =>
            BaptismStatusId.HasValue && Enum.IsDefined(typeof(BaptismStatus), BaptismStatusId.Value)
                ? ((BaptismStatus)BaptismStatusId.Value).ToDisplayName()
                : null;

        public string ClassName { get; set; } = "—";
        public string VillageName { get; set; } = "—";
        public string? VillageHeadman { get; set; }
        public string? VillageChief { get; set; }
        public DateTime? CreationDate { get; set; }
    }
}