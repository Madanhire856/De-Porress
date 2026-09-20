using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SMS.Data;

var builder = WebApplication.CreateBuilder(args);

// --- NEW: Add Entra ID Authentication ---
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// --- NEW: Add Authorization ---
builder.Services.AddAuthorization();

// --- NEW: Add DbContext ---
builder.Services.AddDbContext<SMSDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SMSDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure() // Added retry logic for transient failures
    ));

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

// --- NEW: AUTOMATICALLY CREATE DATABASE AND APPLY MIGRATIONS ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SMSDbContext>();
        // This will create the database if it doesn't exist, and apply any pending migrations.
        context.Database.Migrate();
        Console.WriteLine("Database verified and migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating/migrating the database.");
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
        // You can optionally throw here if you want the app to crash on DB failure
        // throw; 
    }
}
// ---------------------------------------------------------------------------

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// --- NEW: UseAuthentication MUST come before UseAuthorization ---
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();