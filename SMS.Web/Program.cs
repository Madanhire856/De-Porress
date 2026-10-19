using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Middleware;
using SMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("SMSDb")))
    throw new InvalidOperationException("Configure ConnectionStrings:SMSDb using user secrets or AWS Secrets Manager.");
builder.Services.AddOptions<CognitoSettings>().BindConfiguration("Cognito")
    .Validate(c => CognitoSettings.IsHttpsUrl(c.Authority), "Cognito:Authority must be an HTTPS issuer URL.")
    .Validate(c => !string.IsNullOrWhiteSpace(c.ClientId), "Configure Cognito:ClientId.")
    .Validate(c => !string.IsNullOrWhiteSpace(c.ClientSecret), "Configure Cognito:ClientSecret using user secrets or AWS Secrets Manager.")
    .Validate(c => CognitoSettings.IsHttpsUrl(c.Domain), "Cognito:Domain must be an HTTPS login domain.")
    .Validate(c => CognitoSettings.IsHttpsUrl(c.LogoutUri), "Cognito:LogoutUri must be an allowed HTTPS sign-out URL.")
    .Validate(c => !c.InvitationsEnabled || c.HasInvitationAuthority(), "Cognito:Authority must identify the user pool used for invitations.")
    .ValidateOnStart();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "SMS.Cognito.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = false;
})
.AddCookie(CognitoEmailVerification.Scheme, options =>
{
    options.Cookie.Name = "SMS.Cognito.EmailVerification";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Identity/Account/StaffLogin");
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect(options =>
{
    var cognito = builder.Configuration.GetSection("Cognito");
    options.Authority = cognito["Authority"];
    options.ClientId = cognito["ClientId"];
    options.ClientSecret = cognito["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.UseTokenLifetime = true;
    options.SaveTokens = false;
    options.MapInboundClaims = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("email");
    options.Scope.Add("profile");
    if (builder.Configuration.GetValue<bool>("Cognito:InvitationsEnabled"))
        options.Scope.Add("aws.cognito.signin.user.admin");
    options.TokenValidationParameters.NameClaimType = "name";
    options.Events.OnTokenValidated = context =>
    {
        if (!CognitoIdentity.HasVerifiedEmail(context.Principal))
        {
            if (!builder.Configuration.GetValue<bool>("Cognito:InvitationsEnabled")
                || string.IsNullOrWhiteSpace(context.TokenEndpointResponse?.AccessToken))
                context.Fail("A verified staff email address is required.");
            else
                context.Properties.Items[CognitoEmailVerification.TokenKey] = context.TokenEndpointResponse.AccessToken;
        }
        return Task.CompletedTask;
    };
    options.Events.OnTicketReceived = CognitoEmailVerification.TicketReceivedAsync;
    options.Events.OnRedirectToIdentityProviderForSignOut = context =>
    {
        // Cognito uses /logout, client_id and logout_uri rather than OIDC's
        // end_session_endpoint. Sign-out always returns to the configured URL.
        var domain = cognito["Domain"];
        var logoutUri = cognito["LogoutUri"];
        if (!Uri.TryCreate(domain, UriKind.Absolute, out var domainUri) || domainUri.Scheme != "https"
            || !Uri.TryCreate(logoutUri, UriKind.Absolute, out var returnUri)
            || (returnUri.Scheme != "https" && !(builder.Environment.IsDevelopment() && returnUri.IsLoopback)))
            throw new InvalidOperationException("Configure the HTTPS Cognito Domain and allowed LogoutUri.");
        context.Response.Redirect(domain.TrimEnd('/') + "/logout?client_id="
            + Uri.EscapeDataString(options.ClientId!) + "&logout_uri=" + Uri.EscapeDataString(logoutUri));
        context.HandleResponse();
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<ICognitoClientFactory, CognitoClientFactory>();
builder.Services.AddScoped<ICognitoUserService, CognitoUserService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("email-verification", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // CloudFront terminates viewer HTTPS before the demo ALB's HTTP origin.
    // Only enable this behind the origin-header-protected demo listener.
    if (builder.Configuration.GetValue<bool>("ReverseProxy:CloudFront"))
        options.ForwardedProtoHeaderName = "X-DePorress-Viewer-Proto";
    foreach (var network in builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
});
var protection = builder.Services.AddDataProtection().SetApplicationName("DePorress");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
    protection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<WorkspaceAccess>();
builder.Services.AddScoped<WorkspacePageFilter>();
builder.Services.AddScoped<SchoolReportService>();
builder.Services.AddSingleton<IGalleryStore, GalleryStore>();

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

builder.Services.AddControllersWithViews(options => options.Filters.AddService<WorkspacePageFilter>());
builder.Services.AddRazorPages(options =>
{
    foreach (var area in new[] { "Academics", "Config", "Sports", "Finance" })
        options.Conventions.AuthorizeAreaFolder(area, "/");
});

var app = builder.Build();

var migrateOnly = args.Contains("--migrate-only", StringComparer.Ordinal);
var migrateOnStartup = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", builder.Environment.IsDevelopment());
if (migrateOnly || migrateOnStartup)
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
                "No users found. Configure Authentication:BootstrapAdminEmail for the initial administrator.");
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

if (migrateOnly) return;

app.UseForwardedHeaders();

// ALB probes use HTTP and must not redirect to HTTPS or initialize OIDC.
app.Map("/health", health => health.Run(context =>
    context.Response.WriteAsJsonAsync(new { status = "ready" })));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.UseMiddleware<UserProvisioningMiddleware>();

app.MapStaticAssets();

app.MapRazorPages()
.WithStaticAssets();

app.Run();
