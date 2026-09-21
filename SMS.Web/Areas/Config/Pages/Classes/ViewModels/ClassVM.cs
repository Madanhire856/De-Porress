using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Classes.ViewModels
{
    public class ClassVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Please select a grade.")]
        public Guid? GradeId { get; set; }

        [Required(ErrorMessage = "Class name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 200, ErrorMessage = "Capacity must be between 1 and 200.")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Please select a class teacher.")]
        public Guid? ClassTeacherId { get; set; }
    }
}