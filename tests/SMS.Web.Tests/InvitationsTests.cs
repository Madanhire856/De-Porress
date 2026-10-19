using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Users;
using SMS.Web.Areas.Identity.Pages.Account;
using SMS.Web.Services;
using Xunit;

namespace SMS.Web.Tests;

public class InvitationsTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static SMSDbContext Database() => new(new DbContextOptionsBuilder<SMSDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ClaimsPrincipal Principal(bool verified) => new(new ClaimsIdentity([
        new Claim("sub", "subject-1"), new Claim("email", "teacher@example.com"),
        new Claim("email_verified", verified ? "true" : "false")
    ], "test"));
    private static CognitoUserService Service(FakeClient client, bool enabled = true) => new(
        Options.Create(new CognitoSettings { Authority = "https://cognito-idp.eu-north-1.amazonaws.com/eu-north-1_test", InvitationsEnabled = enabled }),
        new Factory(client), NullLogger<CognitoUserService>.Instance);
    private static void Initialize(PageModel page, AuthenticationFake? auth = null)
    {
        var http = new DefaultHttpContext();
        http.RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(auth ?? new()).BuildServiceProvider();
        page.PageContext = new PageContext { HttpContext = http };
        page.TempData = new TempDataDictionary(http, new TempDataProvider());
    }
    private static UpsertModel CreatePage(SMSDbContext db, InvitationFake service, Actor? actor = null)
    {
        var page = new UpsertModel(db, actor ?? new(), service);
        Initialize(page);
        page.user = new() { LoginId = "teacher", Name = "Teacher", Email = "teacher@example.com", RoleId = (int)UserRole.STAFF, IsActive = true };
        return page;
    }

    [Fact]
    public async Task Create_saves_role_and_sends_invitation_without_marking_email_verified()
    {
        using var db = Database(); var service = new InvitationFake(); var page = CreatePage(db, service);
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null));
        var user = Assert.Single(db.Users);
        Assert.Equal((int)UserRole.STAFF, user.RoleId);
        Assert.True(user.IsActive); Assert.False(user.IsEmailConfirmed);
        Assert.Equal(1, service.Invites);
        Assert.NotNull(page.TempData["InvitationStatus"]);
    }

    [Fact]
    public async Task Database_save_failure_keeps_form_values_and_does_not_send_invitation()
    {
        using var db = new SMSDbContext(new DbContextOptionsBuilder<SMSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(new FailedSave()).Options);
        var service = new InvitationFake();
        var page = CreatePage(db, service);
        page.HttpContext.TraceIdentifier = "save-failure-test";

        Assert.IsType<PageResult>(await page.OnPostAsync(null));

        Assert.Equal("teacher@example.com", page.user.Email);
        Assert.Equal(0, service.Invites);
        Assert.Empty(db.Users);
        var error = Assert.Single(page.ModelState[string.Empty]!.Errors).ErrorMessage;
        Assert.Contains("save-failure-test", error);
        Assert.DoesNotContain("private database detail", error);
        Assert.Null(page.TempData["InvitationStatus"]);
    }

    [Theory]
    [InlineData(nameof(User.Name))]
    [InlineData(nameof(User.Mobile))]
    public void User_form_rejects_values_larger_than_database_columns(string property)
    {
        using var db = Database();
        var maxLength = db.Model.FindEntityType(typeof(User))!.FindProperty(property)!.GetMaxLength()!.Value;
        var model = new SMS.Web.Areas.Config.Pages.Users.ViewModels.UserVM
        {
            LoginId = "teacher", Email = "teacher@example.com"
        };
        var field = model.GetType().GetProperty(property)!;
        field.SetValue(model, new string('a', maxLength));
        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), [], true));

        field.SetValue(model, new string('a', maxLength + 1));
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Contains(errors, e => e.MemberNames.Contains(property));
    }

    [Fact]
    public async Task Delivery_failure_keeps_one_saved_user_and_displays_retry_message()
    {
        using var db = Database(); var service = new InvitationFake { Fail = true }; var page = CreatePage(db, service);
        await page.OnPostAsync(null);
        Assert.Single(db.Users);
        Assert.Contains("User created", (string)page.TempData["InvitationError"]!);
        Assert.Null(page.TempData["InvitationStatus"]);
    }

    [Fact]
    public async Task Inactive_new_user_cannot_receive_invitation()
    {
        using var db = Database(); var service = new InvitationFake(); var page = CreatePage(db, service);
        page.user.IsActive = false;
        Assert.IsType<PageResult>(await page.OnPostAsync(null));
        Assert.Empty(db.Users); Assert.Equal(0, service.Invites);
    }

    [Fact]
    public async Task Creation_requires_create_permission_and_blocks_admin_role_escalation()
    {
        using var db = Database(); var service = new InvitationFake();
        var denied = CreatePage(db, service, new Actor { AllowCreate = false });
        Assert.IsType<ForbidResult>(await denied.OnPostAsync(null));
        var escalation = CreatePage(db, service, new Actor { RoleId = (int)UserRole.HEAD });
        escalation.user.RoleId = (int)UserRole.ADMIN;
        Assert.IsType<ForbidResult>(await escalation.OnPostAsync(null));
        Assert.Empty(db.Users); Assert.Equal(0, service.Invites);
    }

    [Fact]
    public async Task Duplicate_email_is_case_insensitive_and_does_not_send_another_invitation()
    {
        using var db = Database(); var service = new InvitationFake(); var first = CreatePage(db, service);
        await first.OnPostAsync(null);
        var second = CreatePage(db, service); second.user.LoginId = "different"; second.user.Email = "TEACHER@example.com";
        Assert.IsType<PageResult>(await second.OnPostAsync(null));
        Assert.Single(db.Users); Assert.Equal(1, service.Invites);
    }

    [Fact]
    public async Task Resend_requires_edit_permission_and_rejects_inactive_users()
    {
        using var db = Database(); var service = new InvitationFake();
        var denied = new DetailsModel(db, new Actor { AllowEdit = false }, service); Initialize(denied);
        Assert.IsType<ForbidResult>(await denied.OnPostInviteAsync(Guid.NewGuid(), Ct));
        var create = CreatePage(db, service); create.SendInvitation = false; create.user.IsActive = false;
        await create.OnPostAsync(null);
        var target = Assert.Single(db.Users);
        var inactive = new DetailsModel(db, new Actor(), service); Initialize(inactive);
        await inactive.OnPostInviteAsync(target.Id, Ct);
        Assert.Contains("Activate", (string)inactive.TempData["InvitationError"]!);
        Assert.Equal(0, service.Invites);
    }

    [Fact]
    public async Task Admin_create_requests_email_delivery_but_does_not_preverify_or_steal_aliases()
    {
        using var client = new FakeClient(); var service = Service(client);
        Assert.Equal(InvitationResult.Sent, await service.InviteAsync(" Teacher@Example.com ", "Teacher", Ct));
        var request = Assert.Single(client.Invites);
        Assert.Equal("teacher@example.com", request.Username);
        Assert.Contains("EMAIL", request.DesiredDeliveryMediums);
        Assert.Equal("false", request.UserAttributes.Single(a => a.Name == "email_verified").Value);
        Assert.False(request.ForceAliasCreation);
        Assert.True(request.TemporaryPassword.Length >= 32);
    }

    [Fact]
    public async Task Existing_confirmed_login_is_not_reset()
    {
        using var client = new FakeClient { Exists = true, Status = UserStatusType.CONFIRMED };
        Assert.Equal(InvitationResult.ExistingLogin, await Service(client).InviteAsync("teacher@example.com", "Teacher", Ct));
        Assert.Single(client.Invites); // Only the attempted create, no RESEND/password reset.
    }

    [Fact]
    public async Task Existing_temporary_login_can_receive_fresh_invitation()
    {
        using var client = new FakeClient { Exists = true, Status = UserStatusType.FORCE_CHANGE_PASSWORD };
        Assert.Equal(InvitationResult.Sent, await Service(client).InviteAsync("teacher@example.com", "Teacher", Ct));
        Assert.Equal(MessageActionType.RESEND, client.Invites[1].MessageAction);
        Assert.True(client.Invites[1].UserAttributes == null || client.Invites[1].UserAttributes.Count == 0);
    }

    [Fact]
    public async Task Disabled_invitation_feature_makes_no_aws_calls()
    {
        using var client = new FakeClient();
        await Assert.ThrowsAsync<CognitoUserOperationException>(() => Service(client, false).InviteAsync("teacher@example.com", null, Ct));
        Assert.Empty(client.Invites);
    }

    [Fact]
    public async Task Verification_rejects_a_token_for_a_different_person()
    {
        using var client = new FakeClient { Subject = "someone-else" };
        await Assert.ThrowsAsync<CognitoUserOperationException>(() => Service(client).VerifyEmailAsync("token", "subject-1", "teacher@example.com", "123456", Ct));
        Assert.Equal(0, client.VerificationCalls);
    }

    [Fact]
    public async Task Verification_requires_cognito_to_report_verified_email()
    {
        using var client = new FakeClient();
        await Service(client).VerifyEmailAsync("token", "subject-1", "teacher@example.com", "123456", Ct);
        Assert.True(client.Verified); Assert.Equal(1, client.VerificationCalls);
        client.Verified = false; client.KeepUnverified = true;
        await Assert.ThrowsAsync<CognitoUserOperationException>(() => Service(client).VerifyEmailAsync("token", "subject-1", "teacher@example.com", "123456", Ct));
    }

    [Fact]
    public async Task Incorrect_code_is_a_safe_actionable_message()
    {
        using var client = new FakeClient { CodeMismatch = true };
        var error = await Assert.ThrowsAsync<CognitoUserOperationException>(() => Service(client).VerifyEmailAsync("secret-token", "subject-1", "teacher@example.com", "123456", Ct));
        Assert.Contains("incorrect", error.Message); Assert.DoesNotContain("secret-token", error.Message);
    }

    [Fact]
    public async Task Unverified_oidc_ticket_issues_only_a_temporary_cookie()
    {
        var auth = new AuthenticationFake();
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider() };
        var props = new AuthenticationProperties(); props.Items[CognitoEmailVerification.TokenKey] = "private-token";
        var context = new TicketReceivedContext(http,
            new AuthenticationScheme("oidc", null, typeof(OpenIdConnectHandler)), new OpenIdConnectOptions(),
            new AuthenticationTicket(Principal(false), props, "oidc")) { ReturnUri = "https://external.example/" };
        await CognitoEmailVerification.TicketReceivedAsync(context);
        Assert.Equal(CognitoEmailVerification.Scheme, Assert.Single(auth.SignedIn));
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, auth.SignedOut);
        Assert.False(CognitoIdentity.HasVerifiedEmail(auth.LastPrincipal));
        Assert.DoesNotContain(auth.LastPrincipal!.Claims, c => c.Type == ClaimTypes.Role);
        Assert.Equal("/", auth.LastProperties!.Items[CognitoEmailVerification.ReturnKey]);
        Assert.False(context.Properties!.Items.ContainsKey(CognitoEmailVerification.TokenKey));
        Assert.True(context.Result!.Handled);
    }

    [Fact]
    public async Task Pending_cookie_does_not_count_as_authenticated_app_user()
    {
        using var db = Database();
        var current = new CurrentUserService(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(false) } }, db);
        Assert.False(current.IsAuthenticated);
        Assert.Null(await current.EnsureUserExistsAsync()); Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Verified_oidc_ticket_removes_temporary_token_without_bypassing_normal_handler()
    {
        var auth = new AuthenticationFake();
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider() };
        var props = new AuthenticationProperties(); props.Items[CognitoEmailVerification.TokenKey] = "private-token";
        var context = new TicketReceivedContext(http,
            new AuthenticationScheme("oidc", null, typeof(OpenIdConnectHandler)), new OpenIdConnectOptions(),
            new AuthenticationTicket(Principal(true), props, "oidc"));
        await CognitoEmailVerification.TicketReceivedAsync(context);
        Assert.Empty(auth.SignedIn);
        Assert.Contains(CognitoEmailVerification.Scheme, auth.SignedOut);
        Assert.False(props.Items.ContainsKey(CognitoEmailVerification.TokenKey));
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Successful_verification_challenges_for_a_fresh_oidc_token()
    {
        var auth = new AuthenticationFake(); var service = new InvitationFake(); var page = new VerifyEmailModel(service);
        Initialize(page, auth);
        var identity = new ClaimsIdentity(Principal(false).Claims, CognitoEmailVerification.Scheme);
        identity.AddClaim(new Claim(CognitoEmailVerification.TokenKey, "private-token"));
        page.HttpContext.User = new ClaimsPrincipal(identity);
        auth.Ticket = new AuthenticationTicket(page.User, new AuthenticationProperties(), CognitoEmailVerification.Scheme);
        page.Code = "123456";
        var challenge = Assert.IsType<ChallengeResult>(await page.OnPostAsync(Ct));
        Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, challenge.AuthenticationSchemes);
        Assert.Empty(auth.SignedIn); Assert.Equal(1, service.Verifications);
        Assert.Contains(CognitoEmailVerification.Scheme, auth.SignedOut);
    }

    [Theory]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("/path\r\nLocation: evil", "/")]
    [InlineData("/Config/Users?x=1", "/Config/Users?x=1")]
    public void Verification_return_url_stays_local(string value, string expected) =>
        Assert.Equal(expected, CognitoEmailVerification.SafeReturnUrl(value));

    private sealed class FailedSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new DbUpdateException("private database detail");
    }

    private sealed class Factory(FakeClient client) : ICognitoClientFactory
    {
        public IAmazonCognitoIdentityProvider CreateAdministrativeClient() => client;
        public IAmazonCognitoIdentityProvider CreateUserClient() => client;
    }
    private sealed class FakeClient() : AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint.EUNorth1)
    {
        public List<AdminCreateUserRequest> Invites { get; } = [];
        public bool Exists, Verified, KeepUnverified, CodeMismatch;
        public string Subject = "subject-1";
        public UserStatusType Status = UserStatusType.CONFIRMED;
        public int VerificationCalls;
        public override Task<AdminCreateUserResponse> AdminCreateUserAsync(AdminCreateUserRequest request, CancellationToken ct = default)
        {
            Invites.Add(request);
            if (Exists && request.MessageAction != MessageActionType.RESEND) throw new UsernameExistsException("exists");
            return Task.FromResult(new AdminCreateUserResponse());
        }
        public override Task<AdminGetUserResponse> AdminGetUserAsync(AdminGetUserRequest request, CancellationToken ct = default) =>
            Task.FromResult(new AdminGetUserResponse { Username = "teacher@example.com", Enabled = true, UserStatus = Status,
                UserAttributes = [new() { Name = "email", Value = "teacher@example.com" }] });
        public override Task<GetUserResponse> GetUserAsync(GetUserRequest request, CancellationToken ct = default) =>
            Task.FromResult(new GetUserResponse { UserAttributes = [new() { Name = "sub", Value = Subject },
                new() { Name = "email", Value = "teacher@example.com" }, new() { Name = "email_verified", Value = Verified ? "true" : "false" }] });
        public override Task<VerifyUserAttributeResponse> VerifyUserAttributeAsync(VerifyUserAttributeRequest request, CancellationToken ct = default)
        {
            VerificationCalls++;
            if (CodeMismatch) throw new CodeMismatchException("private provider message");
            Verified = !KeepUnverified;
            return Task.FromResult(new VerifyUserAttributeResponse());
        }
    }
    private sealed class InvitationFake : ICognitoUserService
    {
        public bool InvitationsEnabled => true;
        public int Invites, Verifications;
        public bool Fail;
        public Task<InvitationResult> InviteAsync(string email, string? name, CancellationToken ct)
        {
            Invites++;
            if (Fail) throw new CognitoUserOperationException("Try again later.");
            return Task.FromResult(InvitationResult.Sent);
        }
        public Task SendVerificationCodeAsync(string token, string sub, string email, CancellationToken ct) => Task.CompletedTask;
        public Task VerifyEmailAsync(string token, string sub, string email, string code, CancellationToken ct) { Verifications++; return Task.CompletedTask; }
    }
    private sealed class Actor : ICurrentUserService
    {
        public bool AllowCreate = true, AllowEdit = true;
        public string? Email => "admin@example.com";
        public string? Name => "Admin";
        public string? IdentitySubject => "admin";
        public bool HasFeatureRight(FeatureRights right) => IsAuthenticated && ((FeatureRightsCatalog.Legacy(Rights) & (long)right) == (long)right);
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public int? RoleId { get; set; } = (int)UserRole.ADMIN;
        public Guid? GroupId => null;
        public long Rights => 0;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights right) => right == AccessRights.CreateUsers ? AllowCreate : right == AccessRights.EditUsers && AllowEdit;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(UserId!.Value);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult(UserId);
    }
    private sealed class TempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    private sealed class AuthenticationFake : IAuthenticationService
    {
        public List<string> SignedIn { get; } = [];
        public List<string> SignedOut { get; } = [];
        public ClaimsPrincipal? LastPrincipal;
        public AuthenticationProperties? LastProperties;
        public AuthenticationTicket? Ticket;
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(Ticket == null ? AuthenticateResult.NoResult() : AuthenticateResult.Success(Ticket));
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        { SignedIn.Add(scheme!); LastPrincipal = principal; LastProperties = properties; return Task.CompletedTask; }
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        { SignedOut.Add(scheme!); return Task.CompletedTask; }
    }
}
