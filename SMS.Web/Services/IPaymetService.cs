using SMS.Data;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Services
{
    /// <summary>
    /// The single entry point for creating or reversing payments.
    /// Every page that touches Payment rows must go through this service —
    /// never directly through the DbContext.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Records a new payment against a student ledger.
        /// Assigns a fresh receipt number atomically and snapshots the
        /// exchange rate and creator's display name.
        /// </summary>
        Task<Payment> RecordAsync(RecordPaymentRequest request);

        /// <summary>
        /// Reverses an existing payment by creating a compensating entry.
        /// The original payment is never modified or deleted — the reversal
        /// is a new row with a negative base amount and a link back to the
        /// original via ReversesPaymentId.
        /// </summary>
        Task<Payment> ReverseAsync(ReversePaymentRequest request);
    }

    // =============================================================
    //  REQUEST DTOs
    // =============================================================

    public class RecordPaymentRequest
    {
        public Guid LedgerId { get; set; }

        /// <summary>Amount paid in the payment currency (e.g. 1200.50).</summary>
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }
        public string CurrencyId { get; set; } = "";

        /// <summary>Rate to base currency. 1 unit of CurrencyId = X base units.</summary>
        public decimal ExchangeRate { get; set; } = 1m;

        public string? RateSource { get; set; }
        public int PaymentMethodId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ProofOfPaymentUrl { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = "";
    }

    public class ReversePaymentRequest
    {
        public Guid OriginalPaymentId { get; set; }
        public string Reason { get; set; } = "";
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = "";
    }
}