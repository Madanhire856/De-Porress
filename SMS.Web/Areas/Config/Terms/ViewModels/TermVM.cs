using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Terms.ViewModels
{
    public class TermVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Academic year is required.")]
        [Range(2000, 2100, ErrorMessage = "Academic year must be between 2000 and 2100.")]
        public int AcademicYear { get; set; } = DateTime.Today.Year;

        [Required(ErrorMessage = "Term number is required.")]
        [Range(1, 3, ErrorMessage = "Term number must be 1, 2, or 3.")]
        public int TermNumber { get; set; } = 1;

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}