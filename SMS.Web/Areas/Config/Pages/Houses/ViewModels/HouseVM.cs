using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Houses.ViewModels
{
    public class HouseVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "House name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "House color is required.")]
        [StringLength(30)]
        public string Color { get; set; } = null!;

        [Required(ErrorMessage = "Please select a house master.")]
        public Guid? MasterId { get; set; }
    }
}