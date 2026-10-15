using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Grades.ViewModels
{
    public class GradeVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Grade name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Please select a grade level.")]
        public int? GradeLevelId { get; set; }
    }
}