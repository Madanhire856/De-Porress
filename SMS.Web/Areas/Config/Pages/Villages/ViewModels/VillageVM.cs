using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Villages.ViewModels
{
    public class VillageVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Village name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Headman name is required.")]
        [StringLength(150)]
        public string Headman { get; set; } = null!;

        [Required(ErrorMessage = "Chief name is required.")]
        [StringLength(150)]
        public string Chief { get; set; } = null!;
    }
}