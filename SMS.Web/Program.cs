using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Middleware;
using SMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<SMSDbContext>((sp, options) =>
{
    options.UseSqlServer(
    builder.Configuration.GetConnectionString("SMSDb"),
    sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    });

options.AddInterceptors(
    sp.GetRequiredService<AuditInterceptor>());

});

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.Configure<RbzApiOptions>(
builder.Configuration.GetSection("RbzApi"));

builder.Services.Configure<RbzFeatureOptions>(
builder.Configuration.GetSection("RbzFeature"));

builder.Services.AddHttpClient<IRbzRateService, RbzRateService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<RbzApiOptions>>().Value;

client.BaseAddress = new Uri(
    opts.BaseUrl.TrimEnd('/') + "/");

    client.Timeout = TimeSpan.FromSeconds(15);

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json");

    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
    {
        client.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {opts.ApiKey}");
    }

});

builder.Services.AddRazorPages()
.AddMicrosoftIdentityUI();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

try
    {
        var context = services.GetRequiredService<SMSDbContext>();

        logger.LogInformation(
            "Checking SMS database and applying pending migrations...");

        await context.Database.MigrateAsync();

        logger.LogInformation(
            "SMS database verified and migrations applied successfully.");

        var hasUsers = await context.Users.AnyAsync();

        if (!hasUsers)
        {
            logger.LogInformation(
                "No users found in the database. The first person to sign in will become the System Administrator.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "An error occurred while creating or migrating the SMS database.");

        Console.WriteLine(
            $"DATABASE ERROR: {ex}");

        throw;
    }

}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<UserProvisioningMiddleware>();

app.MapStaticAssets();

app.MapRazorPages()
.WithStaticAssets();

app.Run();
