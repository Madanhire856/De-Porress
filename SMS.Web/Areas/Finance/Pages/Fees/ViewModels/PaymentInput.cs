using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Finance.Pages.Fees.ViewModels
{
    public class PaymentInput
    {
        public Guid LedgerId { get; set; }

        /// <summary>When the parent actually paid — from the slip / SMS / POS receipt.</summary>
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [Range(0.01, 100_000_000, ErrorMessage = "Enter a valid amount greater than zero.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [Display(Name = "Currency")]
        public string CurrencyId { get; set; } = "";

        /// <summary>
        /// Display-only on the client. The server re-fetches from RBZ on POST
        /// (via cache) and uses its own value — this field is not trusted.
        /// </summary>
        [Display(Name = "Exchange Rate")]
        public decimal ExchangeRate { get; set; } = 1m;

        [Required(ErrorMessage = "Select a payment method.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a payment method.")]
        [Display(Name = "Payment Method")]
        public int PaymentMethodId { get; set; }

        [StringLength(100)]
        [Display(Name = "Reference")]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Proof of Payment URL")]
        public string? ProofOfPaymentUrl { get; set; }

        /// <summary>Set server-side. Not bound from the client.</summary>
        public string? RateSource { get; set; }

        /// <summary>True if the bursar ticked "override rate" — RBZ rate unavailable.</summary>
        [Display(Name = "Override rate")]
        public bool IsRateOverridden { get; set; }

        [StringLength(200)]
        [Display(Name = "Override reason")]
        public string? RateOverrideReason { get; set; }
    }
}