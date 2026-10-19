using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;

namespace SMS.Web.Services;

public sealed partial class SchoolReportService
{
    private async Task<ReportTable> Balances(ReportFilters f, CancellationToken ct)
    {
        var studentIds = Students(f).Select(x => x.Id);
        var q = db.StudentLedgers.AsNoTracking().Where(x => studentIds.Contains(x.StudentId));
        if (f.TermId.HasValue) q = q.Where(x => x.TermId == f.TermId);
        q = f.Status switch { 1 => q.Where(x => x.ClosingBalance > 0), 2 => q.Where(x => x.ClosingBalance == 0), 3 => q.Where(x => x.ClosingBalance < 0), _ => q };
        var rows = await Load(q.Include(x => x.Student).ThenInclude(x => x.Class).Include(x => x.Term)
            .OrderBy(x => x.Student.Surname).ThenBy(x => x.Student.Name).ThenBy(x => x.Term.StartDate).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Learner", "Current class", "Term", "Currency", "Opening balance", "Current closing balance", "Balance state"]);
        foreach (var x in rows) result.Add(Person(x.Student), Text(x.Student.Class?.Name), TermName(x.Term), Text(x.CurrencyId), Money(x.OpeningBalance), Money(x.ClosingBalance), x.ClosingBalance > 0 ? "Outstanding" : x.ClosingBalance < 0 ? "Credit" : "Settled");
        foreach (var group in rows.GroupBy(x => Text(x.CurrencyId)).OrderBy(x => x.Key)) {
            result.Metrics.Add(new($"Outstanding · {group.Key}", Money(group.Sum(x => Math.Max(0, x.ClosingBalance)))));
            result.Metrics.Add(new($"Credit · {group.Key}", Money(group.Sum(x => Math.Max(0, -x.ClosingBalance)))));
        }
        result.Notes.Add("Balances are current ledger values, not balances as of a historical date. Different currencies are never combined. Grade and class labels reflect current placement.");
        if (rows.Any(x => string.IsNullOrWhiteSpace(x.CurrencyId))) result.Notes.Add("Some ledgers have no recorded currency; their totals are listed separately as unspecified (—).");
        return result;
    }
    private static decimal Signed(decimal value, bool reversal) => reversal ? -Math.Abs(value) : value;
    private async Task<ReportTable> Collections(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(db.Payments.AsNoTracking(), x => x.PaymentDate, f);
        var allBursars = access.Can(AccessRights.ViewAllBursarsCashUp);
        if (!allBursars) q = q.Where(x => user.UserId.HasValue && x.CreatorId == user.UserId);
        if (f.TermId.HasValue) q = q.Where(x => x.Ledger != null && x.Ledger.TermId == f.TermId);
        if (f.GradeId.HasValue || f.ClassId.HasValue) {
            var studentIds = Students(f).Select(x => x.Id);
            q = q.Where(x => x.Ledger != null && studentIds.Contains(x.Ledger.StudentId));
        }
        if (f.Status.HasValue) q = q.Where(x => x.IsReversal == (f.Status == 1));
        var rows = await Load(q.Include(x => x.Ledger).ThenInclude(x => x!.Student).Include(x => x.Ledger).ThenInclude(x => x!.Term)
            .OrderBy(x => x.PaymentDate).ThenBy(x => x.ReceiptYear).ThenBy(x => x.ReceiptNumber).ThenBy(x => x.Id), ct);
        var baseCodes = await db.Currencies.AsNoTracking().Where(x => x.IsBase).Select(x => x.Code).ToListAsync(ct);
        var baseCode = baseCodes.Count == 1 ? baseCodes[0] : null;
        decimal? BaseValue(Payment x) => baseCode == null ? null : x.BaseAmount ?? (x.CurrencyId == baseCode ? x.Amount : null);
        var result = new ReportTable(["Payment date", "Receipt year / number", "Learner", "Term", "Recorded by", "Method", "Type", "Currency", "Signed amount", "Base currency", "Signed base amount"]);
        foreach (var x in rows) result.Add(Date(x.PaymentDate), $"{x.ReceiptYear}/{x.ReceiptNumber:D6}", Person(x.Ledger?.Student), TermName(x.Ledger?.Term), Text(x.CreatedByName), ReportCatalog.Name<PaymentMethod>(x.PaymentMethodId), x.IsReversal ? "Reversal" : "Receipt", Text(x.CurrencyId), x.Amount.HasValue ? Money(Signed(x.Amount.Value, x.IsReversal)) : "—", Text(baseCode), BaseValue(x) is decimal amount ? Money(Signed(amount, x.IsReversal)) : "—");
        foreach (var group in rows.Where(x => x.Amount.HasValue).GroupBy(x => Text(x.CurrencyId)).OrderBy(x => x.Key)) {
            result.Metrics.Add(new($"Receipts · {group.Key}", Money(group.Where(x => !x.IsReversal).Sum(x => x.Amount!.Value))));
            result.Metrics.Add(new($"Reversed · {group.Key}", Money(group.Where(x => x.IsReversal).Sum(x => Math.Abs(x.Amount!.Value)))));
            result.Metrics.Add(new($"Net · {group.Key}", Money(group.Sum(x => Signed(x.Amount!.Value, x.IsReversal)))));
        }
        if (baseCode != null) {
            var missing = rows.Count(x => !BaseValue(x).HasValue);
            result.Metrics.Add(new($"Recorded base net · {baseCode}", Money(rows.Sum(x => BaseValue(x) is decimal value ? Signed(value, x.IsReversal) : 0))));
            if (missing > 0) result.Notes.Add($"{missing} record(s) lack a usable base amount and are excluded from the base total. Original-currency amounts are shown where available; foreign amounts are never treated as base amounts.");
        } else result.Notes.Add("A single base currency is not configured. Only separate original-currency totals are available.");
        if (rows.Any(x => !x.Amount.HasValue)) result.Notes.Add("Some records have no original amount; they are excluded from original-currency totals.");
        result.Notes.Add(allBursars ? "Includes all bursars permitted by your school-wide cash-up right." : "Limited to payments and reversals recorded by your own account, matching cash-up permissions.");
        result.Notes.Add("Dates refer to payment dates. Reversals reduce net collections. Class and grade filters use current learner placement.");
        return result;
    }
    private async Task<ReportTable> FeeStructures(ReportFilters f, CancellationToken ct)
    {
        var q = db.FeesStructures.AsNoTracking();
        if (f.GradeId.HasValue) q = q.Where(x => x.GradeId == f.GradeId);
        if (f.TermId.HasValue) q = q.Where(x => x.TermId == f.TermId);
        var rows = await Load(q.OrderBy(x => x.TermId).ThenBy(x => x.GradeId).ThenBy(x => x.Id), ct);
        var grades = await db.Grades.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var terms = await db.Terms.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        var result = new ReportTable(["Grade", "Term", "Levy", "Currency", "Configured amount"]);
        foreach (var x in rows) result.Add(grades.GetValueOrDefault(x.GradeId, "—"), TermName(terms.GetValueOrDefault(x.TermId)), ((LevyType)x.LevyTypeId).ToDisplayName(), Text(x.CurrencyId), Money(x.Amount));
        result.Notes.Add("These are configured amounts per grade and term, not actual money received or a revenue forecast.");
        return result;
    }
}
