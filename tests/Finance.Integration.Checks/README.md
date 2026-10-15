# SQL Server finance integration checks

Run `dotnet run --project tests/Finance.Integration.Checks/Finance.Integration.Checks.csproj`.

Requires local SQL Server Express and Windows authentication. The connection is
hardcoded to the isolated `SMS_FINANCE_UAT_20261015` database on
`localhost\SQLEXPRESS`. The executable applies production EF migrations and adds
synthetic records. It never accepts the application's connection string. Each run
adds a new student, user, term and ledger, retaining the evidence for inspection.
No existing data is deleted. Do not deploy this test executable.

The checks compile the production payment service directly and execute real SQL
transactions, application locks, receipt allocation, validation, corrections,
reversals and four simultaneous retries. They are service integration tests;
they do not establish browser, authentication or role-based UAT acceptance.

A failed assertion produces a nonzero process exit code. The corrected baseline
passes 37 assertions, including rejection of reversed-original correction,
concurrent correction/reversal and duplicate reversal attempts.
