using Microsoft.Data.SqlClient;

var connectionString = Environment.GetEnvironmentVariable("MigrationConnectionString")
    ?? throw new InvalidOperationException("Migration connection secret is missing.");
var password = Environment.GetEnvironmentVariable("RuntimeDatabasePassword")
    ?? throw new InvalidOperationException("Runtime password secret is missing.");
if (password.Length < 32 || password.Length > 64 || password.Any(c => !char.IsAsciiLetterOrDigit(c)))
    throw new InvalidOperationException("Use a 32-64 character alphanumeric runtime password.");

var builder = new SqlConnectionStringBuilder(connectionString);
if (!string.Equals(builder.InitialCatalog, "SMSDB", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Expected the demo SMSDB database.");
builder.InitialCatalog = "master";
builder.Encrypt = true;
builder.TrustServerCertificate = false;
await using (var connection = new SqlConnection(builder.ConnectionString))
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = """
        DECLARE @statement nvarchar(max);
        IF EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'smsapp')
            SET @statement = N'ALTER LOGIN [smsapp] WITH PASSWORD = ' + QUOTENAME(@password, '''');
        ELSE
            SET @statement = N'CREATE LOGIN [smsapp] WITH PASSWORD = ' + QUOTENAME(@password, '''')
                + N', CHECK_POLICY = ON, DEFAULT_DATABASE = [SMSDB]';
        EXEC sys.sp_executesql @statement;
        USE [SMSDB];
        IF DATABASE_PRINCIPAL_ID(N'smsapp') IS NULL
            CREATE USER [smsapp] FOR LOGIN [smsapp];
        IF IS_ROLEMEMBER(N'db_datareader', N'smsapp') <> 1
            ALTER ROLE [db_datareader] ADD MEMBER [smsapp];
        IF IS_ROLEMEMBER(N'db_datawriter', N'smsapp') <> 1
            ALTER ROLE [db_datawriter] ADD MEMBER [smsapp];
        """;
    command.Parameters.Add("@password", System.Data.SqlDbType.NVarChar, 64).Value = password;
    await command.ExecuteNonQueryAsync();
}

// Verify a fresh connection using the restricted account before reporting success.
builder.InitialCatalog = "SMSDB";
builder.UserID = "smsapp";
builder.Password = password;
builder.IntegratedSecurity = false;
await using (var runtime = new SqlConnection(builder.ConnectionString))
{
    await runtime.OpenAsync();
    await using var check = runtime.CreateCommand();
    check.CommandText = "SELECT HAS_PERMS_BY_NAME('dbo.User', 'OBJECT', 'SELECT'), HAS_PERMS_BY_NAME('dbo.User', 'OBJECT', 'INSERT'), HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE'), IS_ROLEMEMBER('db_owner');";
    await using var result = await check.ExecuteReaderAsync();
    if (!await result.ReadAsync() || result.GetInt32(0) != 1 || result.GetInt32(1) != 1
        || result.GetInt32(2) != 0 || result.GetInt32(3) != 0)
        throw new InvalidOperationException("Runtime permission checks failed.");
}
Console.WriteLine("Runtime SQL user smsapp created and verified: data access allowed; table creation and db_owner denied.");
