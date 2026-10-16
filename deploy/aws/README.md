# AWS deployment package

## Demo without a purchased domain

Use `application-demo.yaml` for a synthetic-data demo. It supplies a
`https://...cloudfront.net` address and the default CloudFront certificate.
Build a new image containing the `ReverseProxy:CloudFront` support in Program.cs;
images built before this change will redirect incorrectly behind this template.
Keep DesiredCount at 0 until the migration task has exited successfully.

Fresh SQL Server deployments store JSON payload/audit fields as `nvarchar(max)`.
The initial migration and historical target models use this portable type so
RDS versions without the native `json` type can replay the migration chain.
Already-applied migrations are not rerun; this change does not convert JSON
columns in existing databases. Review those separately before schema upgrades.

The template uses public subnets and public IPs for Fargate outbound access,
avoiding NAT gateways. Task ingress remains limited to its ALB security group;
RDS and EFS remain in the private subnets. Migration tasks must also use the
public subnets with a public IP enabled and the output TaskSecurityGroupId.
PrivateSubnetA/B must be in the same Availability Zones as PublicSubnetA/B.

Generate a private random 32-64 character alphanumeric OriginVerifyToken. The
ALB only forwards requests bearing this token and the HTTPS viewer marker.
CloudFront overwrites both origin headers, forwards viewer Host/cookies/query
strings, and disables caching. Treat the token as a credential; NoEcho masks
parameter displays but authorized users can read origin/listener configuration.
Viewer HTTPS ends at CloudFront: CloudFront-to-ALB traffic uses unencrypted HTTP.
This is a demo tradeoff, not a design for real school or financial data. Use the
custom-domain HTTPS origin template for sensitive workloads.

Set Cognito callback/sign-out URLs from CallbackUrl/LogoutUrl outputs before
starting the service. Run migrations with `--migrate-only`, then use a restricted
SQL principal for runtime. EFS automatic backups are disabled for this demo;
keys are still retained and encrypted. The stack consumes account credits and
does not guarantee Free plan eligibility for every resource. Validate it first:

```powershell
aws cloudformation validate-template --template-body file://deploy/aws/application-demo.yaml --region eu-north-1
```

The app uses your existing Cognito pool in `eu-north-1`. Do not deploy
`cognito.yaml` again unless deliberately creating a different pool.
**No stack has been deployed by Codex. These resources can incur charges.**

## Files

- `database.yaml`: private SQL Server Express RDS instance and security group.
- `application.yaml`: ECS Fargate, ALB/HTTPS, CloudWatch logs, Secrets Manager
  injection and encrypted EFS for shared ASP.NET authentication keys.
- `cognito.yaml`: optional template for a *new* pool, not your existing one.
- `environment.example`: configuration names; contains no real credentials.
- `push-image.ps1`: builds and pushes a versioned x86-64 image to an existing ECR
  repository using your own AWS CLI authentication.

## Prerequisites

Use Stockholm (`eu-north-1`). You need an existing VPC with DNS support, two
public subnets and two private subnets in different Availability Zones. Private
subnets need outbound HTTPS through NAT (or suitable endpoints plus internet
egress for Cognito and external rate APIs). This package does not create a VPC,
NAT gateways, a public domain, ACM DNS validation, or Route 53 records.

Create/validate an ACM certificate for your hostname in the same region. Create
an ECR repository with image scanning enabled and use immutable release tags.
Have Docker and AWS CLI installed/authenticated before running the image script.
Choose instance sizes and verify costs/Free Plan eligibility before deployment.

## Deployment order

1. Back up the original SQL database and record its migration version, user
   roles, fees, payments and ledger totals. Keep the old environment available.
2. Create the database stack with `database.yaml`. Select private subnets; the
   stack generates a strong master password in Secrets Manager and passes it to
   RDS through a dynamic reference. Retrieve it privately using the output secret
   ARN; do not put it in source or command history. SQL Server does not accept
   the CloudFormation DBName option;
   the application migration creates SMSDB. The database has deletion protection,
   backups and snapshot-on-removal. BackupRetentionDays defaults to 1 for the
   Free plan demo; increase it only when the account plan supports longer
   retention. It is single-AZ for small test workloads,
   **not** a production high-availability design. Validate instance/storage
   availability in your account/region before creating it.
3. Restore/migrate existing data through a supported RDS migration process when
   preserving an existing school database. Do not replace data migration with
   creating a fresh empty database. Use a restricted application SQL principal
   after schema deployment; preserve the migration principal separately.
4. Create a Secrets Manager secret (default AWS-managed encryption key) with
   exactly two JSON keys, supplied privately in the AWS console:
   ```json
   {
     "CognitoClientSecret": "YOUR_PRIVATE_APP_CLIENT_SECRET",
     "DatabaseConnectionString": "Server=YOUR_RDS_ENDPOINT,1433;Database=SMSDB;User ID=YOUR_SQL_USER;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False"
   }
   ```
   Use a SQL connection-string builder if the password contains punctuation
   requiring quoting. Do not paste the actual JSON into chat or Git. For a
   customer-managed KMS key, add scoped kms:Decrypt permission to the ECS
   execution role; the template assumes Secrets Manager's default key.
5. Build/push using `./deploy/aws/push-image.ps1 -RepositoryUri YOUR_ECR_URI`.
   Confirm image scanning results before deployment. Repository creation is
   intentionally a separate account action.
6. Create the application stack with `application.yaml`. Supply VPC/subnet IDs,
   the *public ALB subnets'* actual CIDRs, the database security group output,
   certificate, hostname, image URI and Secrets Manager ARN. Leave DesiredCount
   **0** until migrations and configuration are ready. Approve the template's
   IAM role creation when you deliberately deploy it. Existing Cognito IDs are
   provided as defaults. Configure the initial admin email only if needed.
7. Run a one-off ECS task from the stack's TaskDefinitionArn, using the same
   private subnet IDs and TaskSecurityGroupId, with no public IP. Under the `web`
   container override, set **Command** to `--migrate-only` (the image entrypoint
   remains `dotnet SMS.Web.dll`). Use the migration principal in the injected
   database connection. Wait for task exit code **0**; inspect CloudWatch logs.
   Then switch the runtime secret to the restricted application principal.
8. Point your HTTPS hostname to the stack's LoadBalancerDns. Configure that exact
   callback URL (`https://HOST/signin-oidc`) and sign-out URL (`https://HOST/`)
   in the existing Cognito app client. Keep local HTTPS URLs for development.
9. Update DesiredCount to **1**, wait for target health and test the public site,
   sign-in, sign-out, inactive accounts, all role boundaries and finance flows.
   Only cut over after data and UAT acceptance. Scale beyond one task only after
   verifying shared keys, SQL capacity and external service limits.

The ALB only forwards your configured hostname; other HTTPS hosts get 404.
AllowedHosts is wildcarded *behind this restricted listener* to let ALB probes
use the task IP. App task ingress is limited to the ALB security group. Do not
reuse this wildcard for a directly public app host. Forwarded headers trust only
the configured ALB subnet CIDRs. Health checks use `/health`, HTTP 200; this is
a liveness probe, not a database readiness/migration check.

EFS access-point UID/GID 1654 matches the .NET Linux image's non-root APP_UID.
Keys are shared by replicas, encrypted by EFS at rest/in transit, and retained
when the application stack is removed. Protect them as authentication secrets.
Database audit records remain authoritative; optional text-file copies are
disabled in production because container disks are temporary.

The Dockerfile installs AWS's official RDS CA bundle into the Linux trust store;
SQL connections keep `Encrypt=True;TrustServerCertificate=False`. Rebuild images
when AWS rotates the bundle. The build requires access to the image registry,
Ubuntu/Debian package mirrors and truststore.pki.rds.amazonaws.com. For local
Windows connections to RDS, install the appropriate AWS RDS CA in Windows too;
the container trust-store change only applies inside the image. See
https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html.

## Updates and rollback

Take a fresh backup before schema changes. Push a new image tag, run reviewed
migrations as a one-off task, then update ImageUri and wait for deployment health.
The ECS circuit breaker rolls back failed deployments; it does **not** undo SQL
migrations. Keep schema changes backward-compatible with the previous image.
Secrets are injected when a task starts; after secret rotation/replacement force
a new ECS deployment. Never roll back by deleting the database or EFS keys.
The generated database-master secret has no automatic rotation schedule; update
SQL Server's password and its secret together when deliberately rotating it.

Before live provisioning, run (AWS CLI required):
```
aws cloudformation validate-template --template-body file://deploy/aws/database.yaml --region eu-north-1
aws cloudformation validate-template --template-body file://deploy/aws/application.yaml --region eu-north-1
```
Validation does not check account quotas, instance availability, IAM permissions,
network connectivity, pricing, Cognito credentials or a successful deployment.
