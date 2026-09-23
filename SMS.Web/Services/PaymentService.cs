using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SMS.Data;
using SMS.Lib;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly SMSDbContext _context;

        public PaymentService(SMSDbContext context)
        {
            _context = context;
        }

        // =========================================================
        //  RECORD PAYMENT
        // =========================================================
        public async Task<Payment> RecordAsync(RecordPaymentRequest request)
        {
            ValidateRecordRequest(request);

            // Compute the base-currency amount. Round to 2 decimals — money.
            var baseAmount = Math.Round(
                request.Amount * request.ExchangeRate,
                2,
                MidpointRounding.AwayFromZero);

            if (baseAmount <= 0m)
                throw new InvalidOperationException(
                    "Converted amount is too small. Check the exchange rate.");

            var ledger = await _context.StudentLedgers
                .FirstOrDefaultAsync(l => l.Id == request.LedgerId)
                ?? throw new InvalidOperationException("Ledger not found.");

            var currencyExists = await _context.Currencies
                .AnyAsync(c => c.Code == request.CurrencyId);

            if (!currencyExists)
                throw new InvalidOperationException(
                    $"Currency '{request.CurrencyId}' is not configured.");

            var now = DateTime.Now;
            var receiptYear = now.Year;
            var receiptNumber = await AllocateReceiptNumberAsync(receiptYear);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                LedgerId = request.LedgerId,
                Amount = request.Amount,
                CurrencyId = request.CurrencyId,
                ExchangeRate = request.ExchangeRate,
                BaseAmount = baseAmount,
                ReceiptNumber = receiptNumber,
                ReceiptYear = receiptYear,
                RateSource = Trim(request.RateSource),
                IsReversal = false,
                PaymentMethodId = request.PaymentMethodId,
                ReferenceNumber = Trim(request.ReferenceNumber),
                ProofOfPaymentUrl = Trim(request.ProofOfPaymentUrl),
                CreatorId = request.UserId,
                CreatedByName = request.UserDisplayName,
                CreationDate = now
            };

            _context.Payments.Add(payment);

            // Recompute ledger balance from existing payments, then subtract
            // the new base amount manually — EF Core doesn't see the unsaved Add().
            var totalPaidBefore = await _context.Payments
                .Where(p => p.LedgerId == request.LedgerId)
                .SumAsync(p => (decimal?)p.BaseAmount ?? 0m);

            var newClosing = ledger.OpeningBalance - totalPaidBefore - baseAmount;
            ledger.ClosingBalance = newClosing;
            ledger.StatusId = ComputeStatus(newClosing, totalPaidBefore + baseAmount);

            await _context.SaveChangesAsync();

            return payment;
        }

        // =========================================================
        //  REVERSE PAYMENT
        // =========================================================
        public async Task<Payment> ReverseAsync(ReversePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("A reversal reason is required.");

            if (request.Reason.Trim().Length < 10)
                throw new InvalidOperationException(
                    "The reversal reason must be at least 10 characters.");

            var original = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == request.OriginalPaymentId)
                ?? throw new InvalidOperationException("Original payment not found.");

            if (original.IsReversal)
                throw new InvalidOperationException(
                    "Cannot reverse a reversal. Record a new payment instead.");

            var alreadyReversed = await _context.Payments
                .AnyAsync(p => p.ReversesPaymentId == original.Id);

            if (alreadyReversed)
                throw new InvalidOperationException(
                    "This payment has already been reversed.");

            var now = DateTime.Now;
            var receiptYear = now.Year;
            var receiptNumber = await AllocateReceiptNumberAsync(receiptYear);

            var reversal = new Payment
            {
                Id = Guid.NewGuid(),
                LedgerId = original.LedgerId,
                Amount = original.Amount,
                CurrencyId = original.CurrencyId,
                ExchangeRate = original.ExchangeRate,
                BaseAmount = -(original.BaseAmount ?? 0m),
                ReceiptNumber = receiptNumber,
                ReceiptYear = receiptYear,
                RateSource = original.RateSource,
                IsReversal = true,
                ReversesPaymentId = original.Id,
                ReversalReason = request.Reason.Trim(),
                ReversedById = request.UserId,
                ReversedOn = now,
                PaymentMethodId = original.PaymentMethodId,
                ReferenceNumber = original.ReferenceNumber,
                ProofOfPaymentUrl = null,
                CreatorId = request.UserId,
                CreatedByName = request.UserDisplayName,
                CreationDate = now
            };

            _context.Payments.Add(reversal);

            if (original.LedgerId.HasValue)
            {
                var ledger = await _context.StudentLedgers
                    .FirstOrDefaultAsync(l => l.Id == original.LedgerId.Value);

                if (ledger != null)
                {
                    var totalPaidBefore = await _context.Payments
                        .Where(p => p.LedgerId == ledger.Id)
                        .SumAsync(p => (decimal?)p.BaseAmount ?? 0m);

                    var newClosing = ledger.OpeningBalance
                                     - totalPaidBefore
                                     - reversal.BaseAmount!.Value;

                    ledger.ClosingBalance = newClosing;
                    ledger.StatusId = ComputeStatus(
                        newClosing,
                        totalPaidBefore + reversal.BaseAmount!.Value);
                }
            }

            await _context.SaveChangesAsync();

            return reversal;
        }

        // =========================================================
        //  VALIDATION
        // =========================================================
        private static void ValidateRecordRequest(RecordPaymentRequest request)
        {
            if (request.LedgerId == Guid.Empty)
                throw new InvalidOperationException("Ledger is required.");

            if (request.Amount <= 0m)
                throw new InvalidOperationException("Amount must be greater than zero.");

            if (request.ExchangeRate <= 0m)
                throw new InvalidOperationException("Exchange rate must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.CurrencyId))
                throw new InvalidOperationException("Currency is required.");

            if (string.IsNullOrWhiteSpace(request.UserDisplayName))
                throw new InvalidOperationException("User display name is required.");
        }

        // =========================================================
        //  RECEIPT NUMBER ALLOCATION
        // =========================================================
        private async Task<int> AllocateReceiptNumberAsync(int year)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"IF NOT EXISTS (SELECT 1 FROM ReceiptSequence WHERE Year = {0})
                  INSERT INTO ReceiptSequence (Year, LastNumber, LastIssuedOn)
                  VALUES ({0}, 0, GETDATE());",
                year);

            var connection = _context.Database.GetDbConnection();
            var wasClosed = connection.State == ConnectionState.Closed;

            if (wasClosed)
                await connection.OpenAsync();

            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

                cmd.CommandText = @"
                    UPDATE ReceiptSequence
                    SET LastNumber = LastNumber + 1,
                        LastIssuedOn = GETDATE()
                    OUTPUT INSERTED.LastNumber
                    WHERE Year = @Year;";

                var p = cmd.CreateParameter();
                p.ParameterName = "@Year";
                p.DbType = DbType.Int32;
                p.Value = year;
                cmd.Parameters.Add(p);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException(
                        "Failed to allocate a receipt number.");

                return Convert.ToInt32(result);
            }
            finally
            {
                if (wasClosed)
                    await connection.CloseAsync();
            }
        }

        // =========================================================
        //  HELPERS
        // =========================================================
        private static int ComputeStatus(decimal closingBalance, decimal totalPaid)
        {
            if (closingBalance <= 0m)
                return (int)StudentLedgerStatus.SETTLED;

            return totalPaid > 0m
                ? (int)StudentLedgerStatus.PARTIALLY_SETTLED
                : (int)StudentLedgerStatus.OVER_DUE;
        }

        private static string? Trim(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}