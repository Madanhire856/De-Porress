# Move De Porres to AWS

Region from the supplied screenshot: Europe (Stockholm), `eu-north-1`.
Code has been adapted for the Cognito user pool and app client you created.
AWS hosting/RDS have not been provisioned by this task, and end-to-end AWS sign-in
or deployment has not been tested. Provisioning can incur costs;
do not assume the full stack is covered by the database Free Tier.

Local validation: solution build passed; 185 workspace/assignment/identity checks
passed, including verified-email acceptance and rejection of unverified, missing
or empty identity data. Docker and AWS CLI were not available in this workspace,
so the image build and CloudFormation deployment are not verified. Known Cognito
IDs/domain are configured; the client secret remains private. Startup now checks
for missing settings and gives a configuration error without printing values.
Initial checks found the secret/admin settings missing; a subsequent check during
login troubleshooting confirmed the client secret, domain and bootstrap setting
were present. Keep these settings private and save edits in Visual Studio.
The expected file is `%APPDATA%/Microsoft/UserSecrets/1d2cc5b5-8d38-4d84-b06e-2df6abe236ca/secrets.json`.
Do not paste secret values into chat. Rotate a client secret if it has been
shared, and store the replacement privately in that file/AWS Secrets Manager.

The redirect-only smoke test used an explicit placeholder client secret. It
confirmed Cognito discovery/redirect and public/health responses, not token
exchange or user login. The 21 anonymous access checks, 37 finance integration
checks and both JavaScript suites passed after these migration changes.
All 14 runtime checks also passed: health/public responses, logout protections,
the expected Cognito app client/domain, authorization-code flow, PKCE, state and
nonce, required scopes, and preservation of the registered HTTPS callback through
trusted forwarded headers. Evidence: `tests/aws-runtime-results.log`.
All three CloudFormation templates pass the offline YAML/reference/dependency
checks and cfn-lint 1.39.0 schema validation for eu-north-1 (exit code 0).
This does not validate live account quotas, pricing, VPC routes or a deployment.

For the deployment package and precise stack/migration order, use
[deploy/aws/README.md](../deploy/aws/README.md). Production does not migrate on
startup: use the explicit `--migrate-only` one-off task before starting the app.

## From the screen you are on

1. Click AWS Console at the top right.
2. Confirm Stockholm is the selected region.
3. Search for Amazon Cognito. You need a **user pool**, not an identity pool.
4. Create a pool/application for a traditional server-rendered web application.
   Use email sign-in and disable self-service signup: staff accounts are created
   by an administrator. Choose a confidential app client with a client secret.
5. Configure the authorization-code flow with scopes `openid`, `email`, `profile`.
   Configure a Cognito domain and the login-page branding required by the console.
6. For local testing register callback `https://localhost:7246/signin-oidc` and
   sign-out URL `https://localhost:7246/`. For deployment add the exact equivalent
   URLs on the public HTTPS hostname. Cognito requires these URLs to match.
7. Create the first staff user using the email intended for the initial admin.
   Use the normal invitation/password flow and complete email verification.
   The application rejects ID tokens without a verified email address.

Alternatively upload `deploy/aws/cognito.yaml` to CloudFormation in Stockholm.
It creates only the user pool, confidential app client and domain. Enter a unique
domain prefix, callback URL and logout URL. Retrieve the app-client secret in the
Cognito console; it is intentionally not exposed as a stack output. The template
uses the classic hosted UI domain; managed-login branding may be configured
separately in the console. It does not create users, hosting or databases.

## Configure and test sign-in locally

Use .NET user secrets or process environment variables. Do not paste the secret
into chat or commit it. `deploy/aws/environment.example` lists every setting.

- Authority: `https://cognito-idp.eu-north-1.amazonaws.com/POOL_ID` (issuer URL,
  not the login domain).
- ClientId / ClientSecret: the confidential web app client.
- Domain: `https://PREFIX.auth.eu-north-1.amazoncognito.com`.
- LogoutUri: `https://localhost:7246/` for local HTTPS testing.
- Authentication:BootstrapAdminEmail: the verified email for the first admin.

Run the HTTPS launch profile. Use the Staff portal; the existing route now
challenges Cognito instead of Microsoft. HTTP localhost is unsuitable for the
secure authentication cookie. Set the database to an isolated local test DB.

The first **matching configured email**, and only when the SQL User table is
empty, is provisioned as an active administrator. An arbitrary first login no
longer becomes admin. Other new users start inactive with no role and require
activation/role assignment. For an existing database, existing active users keep
their roles by matching their verified email (case-insensitively). Migrating the
database before enabling access preserves their role records. Microsoft account
passwords and sessions are not migrated: users need Cognito credentials.
App sessions use the validated ID token's lifetime rather than extending a
Microsoft-era cookie indefinitely. Tokens are not copied into the app cookie.
Users without a name attribute display their verified email as a fallback.

## Host the app and database

Suggested AWS stack for this .NET 10 container:

| Component | AWS service |
| --- | --- |
| Staff identity | Cognito user pool |
| Container registry | ECR |
| App | ECS Fargate behind an Application Load Balancer |
| Database | RDS for SQL Server Express for eligible small test workloads |
| HTTPS certificate | ACM |
| Secrets | Secrets Manager, injected into the ECS task |
| Runtime logs | CloudWatch Logs |
| Shared authentication keys | Encrypted EFS mount with restricted access |

This is a deployment design, not a fully provisioned infrastructure stack.

1. Create RDS SQL Server in private subnets. Allow port 1433 only from the app's
   security group. Use encrypted connections and check engine/instance eligibility
   and cost in your account. Keep the existing SQL Server provider and migrations.
2. Restore/migrate the current SQL database through a supported SQL Server/RDS
   migration method. Back up and validate balances/user roles before cutover.
3. Build from repository root:
   `docker build -f SMS.Web/Dockerfile -t de-porres .`
   The Dockerfile now copies all referenced project files before restore.
4. Push the image to your ECR repository. Create a Fargate task on port 8080 with
   Secrets Manager values and environment settings from the example file.
5. Connect an HTTPS ALB listener using an ACM certificate; target the app on 8080.
   Health route is `/health`; it returns HTTP 200 without HTTPS redirection or
   authentication. This is liveness only, not a database check. Run the separate
   migration task first. Restrict app ingress to the ALB security group.
6. Set ReverseProxy:KnownNetworks to the actual ALB subnet CIDRs. Forwarded
   headers are processed before HTTPS redirect/authentication. Never trust
   arbitrary public networks. The application template enforces the public
   hostname in the ALB listener and allows task-IP health probe host headers.
7. Mount a persistent, encrypted EFS directory at the configured key path, shared
   by replicas. Restrict permissions; grant the container user write access.
   These keys protect cookies and antiforgery state. Losing them logs users out.
8. Add the deployed HTTPS callback and logout URLs in Cognito and update the
   deployed LogoutUri. Test login/logout before changing DNS or disabling the
   old service. Start with one task after the migration job completes.
9. Configure backups, log retention, budget alerts and deployment rollback.
   Review startup migrations before scaling or using a restricted DB principal.

## Remaining verification

Local builds and policy/payment regression tests do not validate a real Cognito
tenant. Test verified and unverified email, initial admin, existing migrated
accounts, inactive accounts, each staff role, cookie expiry, protected URLs,
logout, hostname/proxy redirects, task restarts and multiple tasks. Validate RDS
data and payments before cutover. Keep the existing environment until acceptance.

References:
- https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-client-apps.html
- https://docs.aws.amazon.com/cognito/latest/developerguide/logout-endpoint.html
- https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-security-best-practices.html
