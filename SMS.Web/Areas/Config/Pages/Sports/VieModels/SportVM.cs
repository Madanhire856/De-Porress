using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Sports.ViewModels
{
    public class SportVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Sport name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;
    }
}