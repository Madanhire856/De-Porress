using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingunsavedmigrationsafterpaymentcorrectioncreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            //  No-op.
            //
            //  Migration 20260927100000_EnablePaymentCorrections already
            //  created the PaymentCorrection table, its two foreign keys,
            //  and all three indexes (PaymentId, CorrectedById,
            //  CorrectedOn). The DDL this migration was scaffolded to
            //  re-emit is redundant and collides with what's already in
            //  the database.
            //
            //  Kept in the history table so the ordering of later
            //  migrations is preserved.
            // ─────────────────────────────────────────────────────────────
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
        }
    }
}