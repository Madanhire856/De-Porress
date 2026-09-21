using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Subjects.ViewModels
{
    public class SubjectVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Subject name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Subject code is required.")]
        [StringLength(30)]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Please select a category.")]
        public int? CategoryId { get; set; }
    }
}