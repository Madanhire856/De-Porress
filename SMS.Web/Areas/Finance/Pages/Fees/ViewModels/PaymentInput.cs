using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Finance.Pages.Fees.ViewModels
{
    public class PaymentInput
    {
        public Guid LedgerId { get; set; }

        [Required]
        [Range(0.01, 100_000_000, ErrorMessage = "Enter a valid amount greater than zero.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [Display(Name = "Currency")]
        public string CurrencyId { get; set; } = "";

        [Required]
        [Range(0.000001, 1_000_000, ErrorMessage = "Enter a valid exchange rate.")]
        [Display(Name = "Exchange Rate")]
        public decimal ExchangeRate { get; set; } = 1m;

        [Required(ErrorMessage = "Select a payment method.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a payment method.")]
        [Display(Name = "Payment Method")]
        public int PaymentMethodId { get; set; }

        [Display(Name = "Reference")]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Proof of Payment URL")]
        public string? ProofOfPaymentUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Rate Source")]
        public string? RateSource { get; set; }
    }
}