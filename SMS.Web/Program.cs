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

// --- Entra ID Authentication ---
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// --- Current User Service ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- Audit interceptor (scoped — resolves its own dependencies lazily) ---
builder.Services.AddScoped<AuditInterceptor>();

// --- DbContext with audit interceptor wired in ---
builder.Services.AddDbContext<SMSDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SMSDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

// --- Payment Service ---
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.Configure<RbzApiOptions>(
    builder.Configuration.GetSection("RbzApi"));
builder.Services.Configure<RbzFeatureOptions>(
    builder.Configuration.GetSection("RbzFeature"));

builder.Services.AddHttpClient<IRbzRateService, RbzRateService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<RbzApiOptions>>().Value;

    client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
});

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

// --- AUTOMATICALLY CREATE DATABASE AND APPLY MIGRATIONS ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SMSDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database verified and migrations applied successfully.");

        if (!context.Users.Any())
        {
            Console.WriteLine("No users in database. The first person to sign in will become the System Administrator.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating/migrating the database.");
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
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