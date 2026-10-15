using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.Currency.ViewModels
{
    public class CurrencyVM
    {
        [Required]
        [StringLength(5)]
        [Display(Name = "Currency Code")]
        public string Code { get; set; } = "";

        [Required]
        [StringLength(15)]
        [Display(Name = "Currency Name")]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(3)]
        [Display(Name = "Symbol")]
        public string Symbol { get; set; } = "";

        [Display(Name = "Set as base currency")]
        public bool IsBase { get; set; }
    }
}