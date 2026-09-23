using Microsoft.EntityFrameworkCore;
using SMS.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Data;

public partial class SMSDbContext
{
    // =============================================================
    //  IMMUTABILITY GUARDS
    //  Block any attempt to modify or delete Payment rows.
    //  Corrections must go through the reversal workflow.
    // =============================================================
    public override int SaveChanges()
    {
        GuardImmutableTables();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GuardImmutableTables();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void GuardImmutableTables()
    {
        var tamperedPayment = ChangeTracker
            .Entries<Payment>()
            .Any(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        if (tamperedPayment)
            throw new InvalidOperationException(
                "Payments are append-only. To correct a mistake, create a reversal entry.");

        var tamperedLedger = ChangeTracker
            .Entries<StudentLedger>()
            .Any(e => e.State == EntityState.Deleted);

        if (tamperedLedger)
            throw new InvalidOperationException(
                "Ledgers cannot be deleted. Reverse the payments against them first.");
    }
}