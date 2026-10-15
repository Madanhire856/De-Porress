using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

// Hardcoded local test database: never accept an application/production connection.
const string connection = "Server=localhost\\SQLEXPRESS;Database=SMS_FINANCE_UAT_20261015;Integrated Security=True;TrustServerCertificate=True";
SMSDbContext Open() => new(new DbContextOptionsBuilder<SMSDbContext>().UseSqlServer(connection).Options);
await using var db = Open();
await db.Database.MigrateAsync();
var user = new User { Id = Guid.NewGuid(), LoginId = Guid.NewGuid().ToString(), Email = "finance-uat@example.invalid", Name = "Synthetic UAT", CreationDate = DateTime.Now, RoleId = 1, IsActive = true };
var student = new Student { Id = Guid.NewGuid(), Name = "Synthetic", Surname = "UAT", GenderId = 1, CategoryId = 1, EnrolmentStatusId = 1 };
var term = new Term { Id = Guid.NewGuid(), Name = "UAT " + Guid.NewGuid().ToString("N"), Number = 1, AcademicYear = 2026, StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(10), CreationDate = DateTime.Now, Creator = user };
if (!await db.Currencies.AnyAsync(c => c.Code == "USD")) db.Currencies.Add(new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", IsBase = true });
var ledger = new StudentLedger { Id = Guid.NewGuid(), Student = student, Term = term, OpeningBalance = 100m, ClosingBalance = 100m, CurrencyId = "USD" };
db.AddRange(user, student, term, ledger);
await db.SaveChangesAsync();
int passed = 0, failed = 0;
void Check(bool condition, string name) { Console.WriteLine($"{(condition ? "PASS" : "FAIL")}: {name}"); if (condition) passed++; else failed++; }
RecordPaymentRequest Request(decimal amount = 25m) => new() { SubmissionId = Guid.NewGuid(), LedgerId = ledger.Id, Amount = amount, CurrencyId = "USD", ExchangeRate = 1m, PaymentDate = DateTime.Today, PaymentMethodId = 3, UserId = user.Id, UserDisplayName = "Synthetic UAT" };
async Task<Payment> Record(RecordPaymentRequest request) { await using var ctx = Open(); return await new PaymentService(ctx).RecordAsync(request); }
async Task Reject(Func<Task> action, string name) { try { await action(); Check(false, name); } catch (InvalidOperationException) { Check(true, name); } }
async Task Balance(decimal expected, string name) { await using var ctx = Open(); var row = await ctx.StudentLedgers.FindAsync(ledger.Id); var sum = await ctx.Payments.Where(p => p.LedgerId == ledger.Id).SumAsync(p => p.BaseAmount ?? 0); Check(row!.ClosingBalance == expected && row.ClosingBalance == row.OpeningBalance - sum, name); }
var request = Request();
var payment = await Record(request);
Check(payment.BaseAmount == 25m && payment.ReceiptNumber > 0, "Payment stores money and allocates receipt");
await Balance(75m, "Payment updates ledger");
var replay = await Record(request);
Check(replay.Id == payment.Id && await db.Payments.CountAsync(p => p.Id == payment.Id) == 1, "Same submission is idempotent");
request.Amount = 26m;
await Reject(() => Record(request), "Changed amount with same submission is rejected");
await Reject(() => Record(Request(0)), "Zero amount rejected");
await Reject(() => Record(Request(-1)), "Negative amount rejected");
var future = Request(); future.PaymentDate = DateTime.Today.AddDays(1);
await Reject(() => Record(future), "Future payment rejected");
var rate = Request(); rate.ExchangeRate = 0;
await Reject(() => Record(rate), "Zero exchange rate rejected");
var tiny = Request(.001m);
await Reject(() => Record(tiny), "Converted amount rounded to zero rejected");
var currency = Request(); currency.CurrencyId = "XXX";
await Reject(() => Record(currency), "Unknown currency rejected");
await Balance(75m, "Rejected requests leave ledger unchanged");
var concurrent = Request(10m);
var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Record(concurrent)));
Check(results.Select(p => p.Id).Distinct().Count() == 1 && await db.Payments.CountAsync(p => p.Id == concurrent.SubmissionId) == 1, "Four concurrent retries create one payment");
await Balance(65m, "Concurrent retry balance remains correct");
await using (var ctx = Open()) {
    await new PaymentService(ctx).CorrectAsync(new CorrectPaymentRequest { PaymentId = payment.Id, Amount = 30m, PaymentDate = DateTime.Today, PaymentMethodId = 3, CorrectionReason = "Synthetic correction for UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" });
}
Check(await db.PaymentCorrections.CountAsync(c => c.PaymentId == payment.Id) == 1, "Correction creates audit history");
await Balance(60m, "Correction recalculates ledger");
var reversalRequest = new ReversePaymentRequest { OriginalPaymentId = payment.Id, Reason = "Synthetic reversal for UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" };
await using (var ctx = Open()) { var reversal = await new PaymentService(ctx).ReverseAsync(reversalRequest); Check(reversal.IsReversal && reversal.BaseAmount == -30m, "Reversal stores compensating amount"); }
await Balance(90m, "Reversal restores balance");
await Reject(async () => { await using var ctx = Open(); await new PaymentService(ctx).ReverseAsync(reversalRequest); }, "Second reversal rejected");
var missingSubmission = Request(); missingSubmission.SubmissionId = Guid.Empty;
await Reject(() => Record(missingSubmission), "Missing submission token rejected");
var missingLedger = Request(); missingLedger.LedgerId = Guid.NewGuid();
await Reject(() => Record(missingLedger), "Unknown ledger rejected");
var missingDate = Request(); missingDate.PaymentDate = default;
await Reject(() => Record(missingDate), "Missing payment date rejected");
var negativeRate = Request(); negativeRate.ExchangeRate = -1m;
await Reject(() => Record(negativeRate), "Negative exchange rate rejected");
var emptyName = Request(); emptyName.UserDisplayName = " ";
await Reject(() => Record(emptyName), "Missing operator display name rejected");
var wrongOwner = Request(10m); wrongOwner.SubmissionId = concurrent.SubmissionId; wrongOwner.UserId = Guid.NewGuid();
await Reject(() => Record(wrongOwner), "Submission replay from another operator rejected");
await Reject(async () => { await using var ctx = Open(); await new PaymentService(ctx).ReverseAsync(new ReversePaymentRequest { OriginalPaymentId = payment.Id, Reason = "short", UserId = user.Id }); }, "Short reversal reason rejected");
await Reject(async () => { await using var ctx = Open(); await new PaymentService(ctx).CorrectAsync(new CorrectPaymentRequest { PaymentId = concurrent.SubmissionId, Amount = 0, PaymentDate = DateTime.Today, CorrectionReason = "Synthetic correction UAT", UserId = user.Id }); }, "Zero correction amount rejected");
await Reject(async () => { await using var ctx = Open(); await new PaymentService(ctx).CorrectAsync(new CorrectPaymentRequest { PaymentId = concurrent.SubmissionId, Amount = 10, PaymentDate = DateTime.Today.AddDays(1), CorrectionReason = "Synthetic correction UAT", UserId = user.Id }); }, "Future correction date rejected");
var fractional = Request(1.01m); fractional.ExchangeRate = .5m;
var converted = await Record(fractional);
Check(converted.BaseAmount == .51m, "Conversion rounds half cents away from zero");
await Balance(89.49m, "Converted payment reconciles to stored ledger");
var independent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Record(Request(1m))));
Check(independent.Select(p => p.Id).Distinct().Count() == 4, "Four simultaneous distinct payments are retained");
Check(independent.Select(p => (p.ReceiptYear,p.ReceiptNumber)).Distinct().Count() == 4, "Simultaneous distinct payments receive unique receipts");
await Balance(85.49m, "Simultaneous distinct payments reconcile ledger");
// Correcting a reversed original must never silently corrupt the accounting sum.
await using (var ctx = Open()) {
    try { await new PaymentService(ctx).CorrectAsync(new CorrectPaymentRequest { PaymentId = payment.Id, Amount = 35m, PaymentDate = DateTime.Today, PaymentMethodId = 3, CorrectionReason = "Correction after reversal UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" }); Check(false, "Reversed original cannot be corrected"); }
    catch (InvalidOperationException) { Check(true, "Reversed original cannot be corrected"); }
}
await Balance(85.49m, "Rejected reversed correction leaves ledger unchanged");
var raceId = independent[0].Id;
var correctionRace = Task.Run(async () => {
    await using var ctx = Open();
    try { await new PaymentService(ctx).CorrectAsync(new CorrectPaymentRequest { PaymentId = raceId, Amount = 2m, PaymentDate = DateTime.Today, PaymentMethodId = 3, CorrectionReason = "Concurrent correction UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" }); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("reversed payment")) { }
});
var reversalRace = Task.Run(async () => {
    await using var ctx = Open();
    await new PaymentService(ctx).ReverseAsync(new ReversePaymentRequest { OriginalPaymentId = raceId, Reason = "Concurrent reversal UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" });
});
await Task.WhenAll(correctionRace, reversalRace);
await Balance(86.49m, "Concurrent correction and reversal preserve ledger balance");
var duplicateReversals = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => {
    await using var ctx = Open();
    try { await new PaymentService(ctx).ReverseAsync(new ReversePaymentRequest { OriginalPaymentId = independent[1].Id, Reason = "Concurrent duplicate reversal UAT", UserId = user.Id, UserDisplayName = "Synthetic UAT" }); return true; }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already been reversed")) { return false; }
}));
Check(duplicateReversals.Count(x => x) == 1, "Concurrent reversals create only one compensating entry");
await Balance(87.49m, "Concurrent reversal attempts reconcile ledger");
Console.WriteLine($"RESULT: {passed} passed, {failed} failed. Synthetic data retained in SMS_FINANCE_UAT_20261015.");
Environment.ExitCode = failed == 0 ? 0 : 1;
