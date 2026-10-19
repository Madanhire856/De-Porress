using System.Security.Cryptography;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;

namespace SMS.Web.Services;

public enum InvitationResult { Sent, ExistingLogin }

public sealed class CognitoUserOperationException(string message) : Exception(message);

public interface ICognitoUserService
{
    bool InvitationsEnabled { get; }
    Task<InvitationResult> InviteAsync(string email, string? name, CancellationToken cancellationToken);
    Task SendVerificationCodeAsync(string accessToken, string subject, string email, CancellationToken cancellationToken);
    Task VerifyEmailAsync(string accessToken, string subject, string email, string code, CancellationToken cancellationToken);
}

public interface ICognitoClientFactory
{
    IAmazonCognitoIdentityProvider CreateAdministrativeClient();
    IAmazonCognitoIdentityProvider CreateUserClient();
}

public sealed class CognitoClientFactory(IOptions<CognitoSettings> settings) : ICognitoClientFactory
{
    public IAmazonCognitoIdentityProvider CreateAdministrativeClient() =>
        new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(settings.Value.Region));
    public IAmazonCognitoIdentityProvider CreateUserClient() =>
        new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint.GetBySystemName(settings.Value.Region));
}

public sealed class CognitoUserService(
    IOptions<CognitoSettings> settings,
    ICognitoClientFactory clients,
    ILogger<CognitoUserService> logger) : ICognitoUserService
{
    public bool InvitationsEnabled => settings.Value.InvitationsEnabled;

    public async Task<InvitationResult> InviteAsync(string email, string? name, CancellationToken cancellationToken)
    {
        RequireEnabled();
        email = email.Trim().ToLowerInvariant();
        try
        {
            using var client = clients.CreateAdministrativeClient();
            try
            {
                await client.AdminCreateUserAsync(new AdminCreateUserRequest
                {
                    UserPoolId = settings.Value.UserPoolId,
                    Username = email,
                    DesiredDeliveryMediums = [DeliveryMediumType.EMAIL],
                    // Cognito emails this password directly. Never return or log it.
                    TemporaryPassword = TemporaryPassword(),
                    ForceAliasCreation = false,
                    UserAttributes = [
                        new() { Name = "email", Value = email },
                        new() { Name = "email_verified", Value = "false" },
                        new() { Name = "name", Value = string.IsNullOrWhiteSpace(name) ? email : name.Trim() }
                    ]
                }, cancellationToken);
                return InvitationResult.Sent;
            }
            catch (UsernameExistsException)
            {
                var existing = await client.AdminGetUserAsync(new AdminGetUserRequest
                {
                    UserPoolId = settings.Value.UserPoolId, Username = email
                }, cancellationToken);
                if (!string.Equals(existing.UserAttributes?.FirstOrDefault(a => a.Name == "email")?.Value,
                    email, StringComparison.OrdinalIgnoreCase))
                    throw new CognitoUserOperationException("That login belongs to a different email. Contact the system administrator.");
                if (existing.Enabled != true)
                    throw new CognitoUserOperationException("This sign-in account is disabled. Contact the system administrator.");
                if (existing.UserStatus == UserStatusType.FORCE_CHANGE_PASSWORD)
                {
                    await client.AdminCreateUserAsync(new AdminCreateUserRequest
                    {
                        UserPoolId = settings.Value.UserPoolId, Username = existing.Username,
                        MessageAction = MessageActionType.RESEND,
                        DesiredDeliveryMediums = [DeliveryMediumType.EMAIL],
                        TemporaryPassword = TemporaryPassword(), ForceAliasCreation = false
                    }, cancellationToken);
                    return InvitationResult.Sent;
                }
                if (existing.UserStatus == UserStatusType.CONFIRMED)
                    return InvitationResult.ExistingLogin;
                throw new CognitoUserOperationException("This email already has a pending sign-in account. Contact the system administrator to finish that account setup.");
            }
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            // Error codes aid operations without recording passwords, tokens or requests.
            logger.LogWarning("Cognito invitation failed with code {ErrorCode}", ex.ErrorCode);
            throw new CognitoUserOperationException("The invitation could not be sent. Check the sign-in service configuration and try Send invitation again.");
        }
        catch (AmazonClientException)
        {
            logger.LogWarning("AWS credentials or connectivity are unavailable for invitations.");
            throw new CognitoUserOperationException("The invitation service is unavailable. Contact the system administrator or try again later.");
        }
    }

    public async Task SendVerificationCodeAsync(string accessToken, string subject, string email, CancellationToken cancellationToken)
    {
        RequireEnabled();
        try
        {
            using var client = clients.CreateUserClient();
            await CheckIdentityAsync(client, accessToken, subject, email, false, cancellationToken);
            await client.GetUserAttributeVerificationCodeAsync(new GetUserAttributeVerificationCodeRequest
            {
                AccessToken = accessToken, AttributeName = "email"
            }, cancellationToken);
        }
        catch (AmazonCognitoIdentityProviderException ex) { throw VerificationError(ex); }
        catch (AmazonClientException) { throw new CognitoUserOperationException("The verification service is unavailable. Try again later."); }
    }

    public async Task VerifyEmailAsync(string accessToken, string subject, string email, string code, CancellationToken cancellationToken)
    {
        RequireEnabled();
        try
        {
            using var client = clients.CreateUserClient();
            await CheckIdentityAsync(client, accessToken, subject, email, false, cancellationToken);
            await client.VerifyUserAttributeAsync(new VerifyUserAttributeRequest
            {
                AccessToken = accessToken, AttributeName = "email", Code = code.Trim()
            }, cancellationToken);
            await CheckIdentityAsync(client, accessToken, subject, email, true, cancellationToken);
        }
        catch (AmazonCognitoIdentityProviderException ex) { throw VerificationError(ex); }
        catch (AmazonClientException) { throw new CognitoUserOperationException("The verification service is unavailable. Try again later."); }
    }

    private static async Task CheckIdentityAsync(IAmazonCognitoIdentityProvider client, string token,
        string subject, string email, bool requireVerified, CancellationToken ct)
    {
        var result = await client.GetUserAsync(new GetUserRequest { AccessToken = token }, ct);
        var attributes = result.UserAttributes ?? [];
        if (attributes.FirstOrDefault(a => a.Name == "sub")?.Value != subject
            || !string.Equals(attributes.FirstOrDefault(a => a.Name == "email")?.Value, email, StringComparison.OrdinalIgnoreCase)
            || (requireVerified && !string.Equals(attributes.FirstOrDefault(a => a.Name == "email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase)))
            throw new CognitoUserOperationException("Your sign-in information changed. Start sign-in again.");
    }

    private CognitoUserOperationException VerificationError(AmazonCognitoIdentityProviderException ex)
    {
        logger.LogWarning("Cognito email verification failed with code {ErrorCode}", ex.ErrorCode);
        return new(ex switch
        {
            CodeMismatchException => "That code is incorrect. Check your email and try again.",
            ExpiredCodeException => "That code has expired. Request a new code.",
            TooManyRequestsException or LimitExceededException => "Too many attempts. Wait a few minutes before trying again.",
            NotAuthorizedException => "Your verification session has expired or is unavailable. Start sign-in again. Contact the administrator if this continues.",
            _ => "Email verification is unavailable right now. Try again later or contact the administrator."
        });
    }

    private void RequireEnabled()
    {
        if (!InvitationsEnabled) throw new CognitoUserOperationException("User invitations are not enabled for this environment.");
    }

    private static string TemporaryPassword() => "Aa1!" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
}
