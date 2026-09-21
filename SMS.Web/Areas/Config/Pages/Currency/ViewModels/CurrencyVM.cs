using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Currency.ViewModels
{
    public class CurrencyVM
    {
        [Required(ErrorMessage = "Currency code is required.")]
        [StringLength(10)]
        [Display(Name = "Code")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Currency name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Currency symbol is required.")]
        [StringLength(10)]
        public string Symbol { get; set; } = null!;
    }
}