using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Staff.ViewModels
{
    public class StaffVM
    {
        // ==========================================================
        //  IDENTITY
        // ==========================================================
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        // ==========================================================
        //  PERSONAL DETAILS
        // ==========================================================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(100)]
        public string Surname { get; set; } = null!;

        [Required(ErrorMessage = "ID Number is required.")]
        [StringLength(30)]
        public string IdNumber { get; set; } = null!;

        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Please select a gender.")]
        public int? GenderId { get; set; }

        [Required(ErrorMessage = "Please select a marital status.")]
        public int? MaritalStatusId { get; set; }

        // ==========================================================
        //  EMPLOYMENT
        // ==========================================================

        [Required(ErrorMessage = "EC Number is required.")]
        [StringLength(30)]
        public string EcNumber { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? DateJoined { get; set; } 

        [Required(ErrorMessage = "Please select a category.")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a qualification.")]
        public int? QualificationId { get; set; }

        // ==========================================================
        //  STATUS
        // ==========================================================
        public bool IsActive { get; set; }
    }
}