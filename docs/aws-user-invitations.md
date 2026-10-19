# User invitations and email verification

An administrator can create a local account and send its Cognito invitation from
**Config > Users > Invite user**. New accounts default to sending an invitation.
The existing create/edit and role-assignment permission checks still apply.
Inactive accounts cannot receive invitations. Editing a local profile does not
automatically change a Cognito account or send an invitation.

The user receives a temporary password, opens the app's sign-in page and sets a
new password in Cognito. If Cognito's ID token still reports an unverified email,
the app opens **Verify your email**. The user requests an emailed code and enters
it. The app verifies the code with Cognito, then performs a fresh OIDC sign-in to
obtain the verified claim. It never sets `email_verified=true` administratively.

## One-time configuration for the existing AWS deployment

1. Open the **existing Cognito app client** that matches the deployed client ID.
   In its managed-login OAuth settings, allow the scope
   `aws.cognito.signin.user.admin` alongside `openid`, `email` and `profile`.
   Preserve the current callback/sign-out URLs and authorization-code flow.
   This scope authorizes a signed-in user to verify their own email. It does not
   give them application administrator rights or IAM permissions.
2. In the user pool's invitation message template, include the username
   placeholder `{username}`, temporary-password placeholder `{####}` and the
   app login URL. For the demo, use:

   ```text
   Your school account is ready.
   Sign in at https://d2mwmphbhofdh6.cloudfront.net/Identity/Account/StaffLogin
   Username: {username}
   Temporary password: {####}
   Set a new password, then verify your email to open your workspace.
   ```

   Check the pool's email sender/delivery configuration and quotas. The code
   reports delivery failures without claiming that an invitation succeeded.
   Keep public self-registration disabled for this staff-only system.
3. Build and push a **new** image tag. No SQL migration is required for this
   feature. For the current deployment, `demo-v4` is a possible next unused tag.
4. Validate and update the **existing application stack with the revised local
   `application-demo.yaml`**, rather than selecting "Use existing template".
   Use `application.yaml` instead if that is your established HTTPS-domain path.
   Set ImageUri to the new image. Preserve DesiredCount, BootstrapAdminEmail,
   secret ARN, origin token and all network parameters.
5. Review the IAM change. The application **task role**, not its execution role,
   gains only `cognito-idp:AdminCreateUser` and `cognito-idp:AdminGetUser`, scoped
   to the pool from CognitoAuthority. The revised templates set
   `Cognito__InvitationsEnabled=true`. Do not apply this template with an older
   image that lacks the verification flow.
6. Wait for the deployment to become healthy. Test an invitation to a mailbox you
   control, password setup, email-code delivery, a wrong/expired code, and normal
   sign-in after verification. Check that a user without CreateUsers cannot
   invite anyone and that non-system-admins cannot assign the ADMIN role.

`cognito.yaml` includes the scope and invitation-message settings for a **new**
pool. Do not redeploy it to replace your existing pool. Its AppLoginUrl parameter
must point at the deployed app, not the default development address.

For another environment, enable `Cognito:InvitationsEnabled` only after its app
client scopes, invitation email and AWS task permissions are ready. Invitations
default to disabled outside the revised AWS templates.

## Retry and existing-account behavior

The local account saves before the email request. If AWS fails, the page says
that the user saved but the invitation was not confirmed. Open that account's
details and choose **Send invitation** to retry. This requires both CreateUsers
and EditUsers, a permitted target role, and an active account.

An existing Cognito user still waiting to change a temporary password receives a
fresh invitation. The previous temporary password expires. An existing confirmed
login keeps its password and receives no password reset. Other pending/disabled
states produce an actionable message for the system administrator. The app never
deletes an existing Cognito user, takes over an alias, or silently changes an
existing user's email verification status.

The app matches an existing local user by verified email, as before. The
invitation feature does not modify that account-linking design. Additional
schools/tenants would need a separate tenancy and subject-linking design.

## Session and credential handling

The OIDC flow finishes issuer/signature/nonce validation before it issues a
temporary verification ticket. That ticket uses a separate, protected, HttpOnly,
Secure cookie and expires after ten minutes. It contains the user's Cognito
access token for verification calls only. The app's default authentication
scheme does not accept it for normal pages. It carries no application roles.
Unverified principals cannot auto-provision local users or access workspaces.

Verification forms use Razor antiforgery protection. Requests have a per-user
limit and repeated code sends have a one-minute cooldown. Cognito's own code and
request limits also apply. After verification, the app clears the temporary
cookie and requests a fresh OIDC token. It does not manufacture a verified app
session. Access tokens and temporary passwords are never returned in page text
or application log messages.

The AWS SDK uses the ECS task role through its standard credential chain. Do not
add AWS access keys to appsettings or the runtime secret. User verification APIs
use the user's scoped access token and do not require additional IAM actions.

## Checks

```powershell
dotnet test tests/SMS.Web.Tests/SMS.Web.Tests.csproj
dotnet build SMS.Web/SMS.Web.csproj
```

Automated tests use a fake Cognito client and in-memory database. They do not
send actual invitations. A deployed mailbox test remains necessary to verify
the real pool, client scopes, email sender and IAM role configuration.

References:

- https://docs.aws.amazon.com/cognito/latest/developerguide/how-to-create-user-accounts.html
- https://docs.aws.amazon.com/cognito-user-identity-pools/latest/APIReference/API_GetUserAttributeVerificationCode.html
- https://docs.aws.amazon.com/cognito-user-identity-pools/latest/APIReference/API_VerifyUserAttribute.html
- https://docs.aws.amazon.com/cognito/latest/developerguide/signing-up-users-in-your-app.html
