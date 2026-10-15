# AWS migration record — St Martin De Porres Primary School

**Date:** 15 October 2026, Africa/Harare  
**Application:** De-Porress / SMS.Web, ASP.NET Core Razor Pages, .NET 10  
**AWS region:** Europe (Stockholm), `eu-north-1`

## 1. Current position

The application code was changed from Microsoft Entra sign-in to Amazon Cognito.
The user created the Cognito user pool, web app client and first user in AWS.
Deployment templates and instructions were prepared for AWS hosting and SQL
Server. Local build, policy, payment, redirect and template checks passed.

**This is not yet a completed cloud deployment.** No ECS application stack or RDS
database was deployed during this work, and no existing school database was
transferred to AWS. A complete successful staff login and logout have not been
confirmed. The most recent reported issue was Cognito rejecting logout; the
required sign-out URL was provided, but its resolution has not been confirmed.

This record distinguishes changes made to code, AWS console steps reported by
the user, local test evidence, and work still outstanding.

## 2. Why the change was requested

The original app used Microsoft identity services and SQL Server. Difficulty
obtaining a test SQL database through the user's Azure account prompted a review
of AWS database options. The decision then expanded to moving identity, hosting
and the database to AWS.

SQL Server was retained to preserve existing Entity Framework migrations,
student ledgers, payments and other data models. Migrating to PostgreSQL or MySQL
was not part of this work. AWS hosting does not require changing the .NET/Razor
application framework. The existing exchange-rate API remains an external
application integration; it was not replaced with an AWS service.

AWS Free Tier/database eligibility was discussed, but no assurance was given that
the complete hosting stack is free. Pricing, credits, quotas and region/instance
eligibility must be checked in the account before resources are created.

## 3. AWS resources and settings established

The user navigated from the AWS project settings page into AWS Console and
created a Cognito application/user pool. This is separate from AWS account/project
membership: console members administer AWS, while Cognito users sign in to SMS.

| Item | Recorded value |
| --- | --- |
| User pool display name | User pool - 8kh0dr |
| User pool ID | `eu-north-1_LOhGBF9su` |
| App client display name | St Martin De Porress Primary Scool (console spelling) |
| App client ID | `6ulsn0evv7q8qmoeo69al441sg` |
| Application type | Traditional web application |
| Issuer / Authority | `https://cognito-idp.eu-north-1.amazonaws.com/eu-north-1_LOhGBF9su` |
| Managed-login domain | `https://eu-north-1lohgbf9su.auth.eu-north-1.amazoncognito.com` |
| Local callback URL | `https://localhost:7246/signin-oidc` |
| Local allowed sign-out URL | `https://localhost:7246/` |
| OAuth grant | Authorization code |
| Required OIDC scopes | `openid`, `email`, `profile` |

Email-only sign-in and disabled public self-registration were the instructed
configuration. The console showed a Cognito user-pool identity provider and
managed-login styling assigned to the app client. Scope and URL edits were
guided step by step. Their final live console state was not independently
audited through an AWS API.

The user reported creating the first Cognito user, sending an invitation and
receiving the temporary password. A temporary password is handled only in the
Cognito login flow, including any required change to a permanent password.
Neither password is stored in the application source or this document.

The AWS setup wizard displayed sample Go code. That sample was not integrated;
the existing .NET application uses its own OpenID Connect implementation.

## 4. Application changes

### Authentication and identity

- Replaced Microsoft.Identity.Web/Microsoft.Identity.Web.UI authentication with
  standard ASP.NET Core cookie and OpenID Connect authentication for Cognito.
- Configured authorization-code flow with PKCE, issuer/audience/signature
  validation through the OIDC middleware, and scopes `openid email profile`.
- Kept the public Staff portal route; it now redirects to Cognito.
- Required a nonempty email claim with `email_verified=true` after ID-token
  validation. Unverified users cannot complete app sign-in.
- Used Cognito's `sub` claim as the identity subject and renamed the app interface
  property from AzureId to IdentitySubject.
- Matched existing SQL user records by verified email, case-insensitively, to
  preserve their active status, roles and groups. Email should be kept consistent
  when creating Cognito accounts for migrated staff.
- Used the verified email as a display-name fallback where no name claim exists.
- Kept authentication cookies secure and HTTP-only. Disabled sliding expiration,
  used the validated ID token's lifetime, and did not save tokens in the cookie.

Microsoft passwords, sessions and Microsoft app-registration settings were not
transferred into Cognito. Existing staff need Cognito accounts/credentials.

### Administrator and rights behavior

The earlier unrestricted "first login becomes administrator" behavior was
replaced by a restricted bootstrap:

1. The SQL application's User table must be empty.
2. The login must have a verified email.
3. That email must match `Authentication:BootstrapAdminEmail`.

Only then is the new SQL user active with the ADMIN role. The ADMIN role receives
all application rights through the existing rights service. Other newly
provisioned users start inactive with no role; an administrator must activate
them and assign the appropriate role/group. Existing users retain their SQL
roles; the bootstrap setting does not automatically promote an existing user.

Being a Cognito user, AWS account owner, or AWS console administrator does not by
itself grant school-application administration rights.

### Logout

- Kept logout as an app POST protected by antiforgery validation.
- Made app GET logout return HTTP 405.
- Cleared the local application cookie before redirecting to Cognito's `/logout`.
- Sent `client_id` and the configured `logout_uri` to Cognito.
- Restricted app return URLs to local paths; the Cognito exit uses its fixed,
  configured allowed sign-out URL.

### Production/runtime preparation

- Added startup validation for required Cognito configuration and the database
  connection setting, with messages that do not print secret values.
- Added production configuration that clears the local SQL connection and local
  logout URL so deployment must supply them explicitly.
- Processed trusted forwarded headers before HTTPS/authentication to preserve
  HTTPS callback URLs behind an AWS load balancer.
- Added `/health`, an HTTP 200 liveness endpoint that does not redirect to HTTPS
  or initialize authentication. It is not a database readiness check.
- Added shared, persistent ASP.NET Data Protection key storage for replicas.
- Disabled optional audit text-file copies in production; SQL audit records
  remain authoritative and runtime logs can go to CloudWatch.
- Disabled production startup migrations by default. Added `--migrate-only` for
  an explicit one-off schema deployment task. Development retains startup
  migrations unless its configuration disables them.

### Container preparation

- Fixed Docker restore ordering to include SMS.Web, SMS.Lib and SMS.Data project
  files before package restore.
- Set the container HTTP port to 8080 and retained non-root runtime execution.
- Added AWS's official RDS CA bundle to the Linux trust store so SQL connections
  can keep `Encrypt=True;TrustServerCertificate=False`.
- Excluded local build output, test artifacts, editor/account directories and
  credential/certificate files from the Docker build context.

The Docker image itself has not been built here because Docker is unavailable.

## 5. Configuration and secret handling

Non-secret pool/client identifiers and the known login domain were added to
application configuration. Private values belong in Visual Studio user secrets
for development and AWS Secrets Manager for deployment.

Typical local settings (placeholders only):

```json
{
  "Cognito:ClientSecret": "PRIVATE_VALUE_STORED_LOCALLY",
  "Cognito:Domain": "https://eu-north-1lohgbf9su.auth.eu-north-1.amazoncognito.com",
  "Authentication:BootstrapAdminEmail": "YOUR_VERIFIED_ADMIN_EMAIL"
}
```

Open these through **SMS.Web → Manage User Secrets** in Visual Studio. The
project's UserSecretsId is `1d2cc5b5-8d38-4d84-b06e-2df6abe236ca`, with expected
file location:

```text
%APPDATA%/Microsoft/UserSecrets/1d2cc5b5-8d38-4d84-b06e-2df6abe236ca/secrets.json
```

Initial disk checks did not find the new keys in that file. After the user
confirmed/saved the entries, a later check confirmed the client secret, Cognito
domain and bootstrap setting were present. Presence was checked without printing
their values; it does not prove that the secret matches the current AWS client.

A client secret was pasted into chat during setup. Its value is intentionally
excluded from this record. Replacing that secret in Cognito and privately updating
the local/deployed configuration was advised. Completion of replacement has not
been confirmed. Do not put actual passwords, secrets, tokens or authentication
codes into Git, documentation or screenshots.

## 6. Login/logout troubleshooting record

| Reported issue | Finding and action | Confirmed position |
| --- | --- | --- |
| Cognito "Something went wrong" | User supplied `error=redirect_mismatch`. Checked the local request: HTTPS app generated `https://localhost:7246/signin-oidc`. Guided exact Allowed callback URL and HTTPS launch-profile configuration. | A later callback error showed the flow had progressed past that mismatch. |
| `invalid_request` / `invalid_scope` | App requests `openid email profile`; earlier console output listed `email openid phone`. Guided enabling profile and saving matching scopes. | A later token-verification error showed the flow progressed further. |
| "A verified staff email address is required" | App's post-token-validation guard rejected an absent/false email verification claim. Guided checking the Cognito user's Email verified status. | Final verified-email correction and successful app session were not confirmed. |
| Logout "Invalid request" | Effective local LogoutUri was `https://localhost:7246/`. Guided adding that exact value, including trailing slash, to Allowed sign-out URLs. | Final successful logout was not confirmed. |

Start login from the running app's **Staff portal**, using the HTTPS profile at
`https://localhost:7246`. Reusing a previous error URL does not start a new login.
The console's View login page is useful for viewing Cognito but does not replace
the app's state/correlation handling for a complete login.

## 7. Testing and security work done alongside migration

An initial local test/UAT pass preceded the migration implementation. It found
anonymous management-list access, a service-level reversed-payment correction
gap, logout concerns, and vulnerable cryptography package versions.

The user then authorized corrections:

- Required authentication for protected areas and registered the workspace
  access filter globally. Anonymous management pages now challenge sign-in.
- Rejected correction of reversed originals in PaymentService, and serialized
  recording/correction/reversal per ledger using SQL application locks and
  transactions. Concurrent reversal attempts are checked.
- Hardened logout as described above.
- Updated System.Security.Cryptography.Xml and Pkcs to 10.0.13. The dependency
  audit run at that stage reported no known vulnerable packages in the three
  application projects; this was not a complete penetration test.

These payment/access fixes were related release-readiness work, not prerequisites
specific to Cognito. Reversed payments remain in accounting history; to reinstate
a mistakenly reversed payment, record a new payment instead of editing history.

### Recorded final local results

| Check | Result / limit |
| --- | --- |
| Solution build | Passed: 0 errors, 32 warnings; compiler warnings remain |
| Identity, workspace and assignment checks | 185 passed |
| SQL finance integration | 37 passed in a separate synthetic database |
| Anonymous protected-route checks | 21 passed; redirects target configured Cognito |
| AWS runtime checks | 14 passed: public/health, logout guards, correct client/domain, code flow, PKCE, state/nonce, scopes and HTTPS callback |
| Finance chart and submit-lock JavaScript | Both suites passed |
| Template YAML, references and dependency graph | All three templates passed |
| CloudFormation schema checks | cfn-lint 1.39.0, eu-north-1: exit code 0 |
| Docker build / cloud deployment | Not executed |
| Complete authenticated staff UAT | Not completed |

The automated redirect smoke test used a clearly identified placeholder client
secret and did **not** exchange tokens or sign in a user. This is why passing
redirect tests did not establish that actual Cognito scopes, email verification,
the private secret or AWS console URL settings were correct.

Local tests used `SMS_UAT_20261015` and `SMS_FINANCE_UAT_20261015`, not a deployed
RDS database. Temporary test servers were stopped after testing. Synthetic test
data and local evidence were retained. Existing unrelated project-file edits
were preserved; no commits or deployment were performed as part of this work.

Evidence files include `tests/aws-release-build.log`,
`tests/aws-complete-workspace.log`, `tests/aws-complete-finance.log`,
`tests/aws-complete-anonymous.log`, `tests/aws-runtime-results.log`,
`tests/aws-template-validation.log` and `tests/aws-cfn-lint-results.log`.

## 8. Prepared AWS architecture and files

| Component | Prepared AWS target | Status |
| --- | --- | --- |
| Staff sign-in | Cognito | Pool/client/user created by user; final login/logout acceptance pending |
| App image | ECR | Build/push script prepared; image not built/pushed here |
| App hosting | ECS Fargate | CloudFormation template prepared; not deployed |
| HTTPS entry | Application Load Balancer + ACM | Template prepared; hostname/certificate/network still required |
| Database | Private RDS SQL Server Express for test scale | Template prepared; not provisioned/migrated |
| Runtime secrets | Secrets Manager | Injection prepared; cloud secrets still need creation |
| Authentication key persistence | Encrypted EFS | Template prepared; not deployed |
| Runtime logging | CloudWatch Logs | Template prepared; not deployed |

Key source and handover files:

| File | Purpose |
| --- | --- |
| `SMS.Web/Program.cs` | Cognito setup, cookies, proxy handling, migrations and health |
| `SMS.Web/Services/CognitoIdentity.cs` | Verified-email guard |
| `SMS.Web/Services/CognitoSettings.cs` | Configuration validation |
| `SMS.Web/Services/CurrentUserService.cs` | Claims, matching and bootstrap behavior |
| `SMS.Web/Areas/Identity/Pages/Account/StaffLogin.cshtml.cs` | App login entry |
| `SMS.Web/Areas/Identity/Pages/Account/Logout.cshtml.cs` | Protected app logout |
| `SMS.Web/appsettings.Production.json` | Production defaults requiring external configuration |
| `SMS.Web/Dockerfile` and `.dockerignore` | Container build and exclusions |
| `deploy/aws/application.yaml` | ECS, ALB, EFS, IAM and logs; uses existing Cognito/RDS/network |
| `deploy/aws/database.yaml` | Private RDS instance, generated master secret and security group |
| `deploy/aws/cognito.yaml` | Optional **new** pool template; do not redeploy to replace the existing pool accidentally |
| `deploy/aws/environment.example` | Deployment configuration names, no real secret values |
| `deploy/aws/push-image.ps1` | Build/push to an existing ECR repository |
| `deploy/aws/README.md` | Deployment order, prerequisites and rollback notes |
| `docs/aws-migration.md` | Configuration/setup reference |
| `docs/uat-2026-10-15.md` | Original UAT findings and remediation |

The application template expects an existing VPC, public/private subnets, outbound
connectivity, a hostname and an ACM certificate. It does not create that entire
network or register DNS. Its initial ECS DesiredCount is zero so migrations can
be reviewed/run before enabling the service. The RDS template is a small,
single-AZ test design, not a production high-availability guarantee.

## 9. Remaining work before AWS cutover

1. Replace the exposed client secret and privately update configuration.
2. Confirm the initial Cognito user's verified email and complete a successful
   app login, required password change, administrator assignment and logout.
3. Confirm every staff role, inactive-user handling and direct URL restrictions
   in authenticated browser UAT; verify bootstrap against the intended SQL DB.
4. Choose/prepare the AWS network, public hostname and ACM certificate; review
   actual service costs and account permissions/quotas.
5. Provision RDS and migrate existing school data, preserving users, roles,
   payments and ledgers. Reconcile totals and keep a recoverable backup.
6. Build and scan the container, push to ECR and provision the application stack
   with the private runtime secret. Run the separate `--migrate-only` task and
   inspect its successful exit before starting the service.
7. Register the deployed HTTPS callback/sign-out URLs in the existing Cognito
   client and update the deployed LogoutUri.
8. Verify RDS TLS, application health, proxy redirects, EFS keys across task
   restarts/replicas, logging, backup restoration, performance and business UAT.
9. Cut over DNS only after acceptance. Retain the previous environment until
   rollback and data recovery have been reviewed.

An ECS deployment rollback does not undo database migrations. Keep schema changes
compatible with the previous image, retain backups and never use deletion of RDS
or authentication keys as a rollback method.

## 10. Official references

- [Cognito app-client configuration](https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-client-apps.html)
- [Cognito authorization endpoint](https://docs.aws.amazon.com/cognito/latest/developerguide/authorization-endpoint.html)
- [Cognito logout endpoint](https://docs.aws.amazon.com/cognito/latest/developerguide/logout-endpoint.html)
- [RDS TLS certificates](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html)

The status statements above record this project's work and user reports. They
are not a claim that all listed AWS services have been provisioned or accepted.
