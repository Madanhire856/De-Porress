param(
    [string]$Region = 'eu-north-1',
    [string]$StackName = 'de-porress-demo-app',
    [Parameter(Mandatory)][string]$RuntimeSecretArn,
    [Parameter(Mandatory)][string[]]$PublicSubnets
)
$ErrorActionPreference = 'Stop'
$stackJson = & aws cloudformation describe-stacks --stack-name $StackName --region $Region --output json --no-cli-pager
if ($LASTEXITCODE -ne 0) { throw 'Cannot read application stack.' }
$outputs = @{}
($stackJson | ConvertFrom-Json).Stacks[0].Outputs | ForEach-Object { $outputs[$_.OutputKey] = $_.OutputValue }
$definitionJson = & aws ecs describe-task-definition --task-definition $outputs.TaskDefinitionArn --region $Region --output json --no-cli-pager
if ($LASTEXITCODE -ne 0) { throw 'Cannot read task definition.' }
$definition = ($definitionJson | ConvertFrom-Json).taskDefinition
$web = $definition.containerDefinitions | Where-Object { $_.name -eq 'web' }
$source = [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $PSScriptRoot 'runtime-user/Program.cs')))
$project = [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $PSScriptRoot 'runtime-user/RuntimeUser.csproj')))
$shell = @'
set -eu
if ! command -v curl >/dev/null; then apt-get update -qq && apt-get install -y -qq curl ca-certificates; fi
curl --fail --silent --show-error https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem -o /tmp/rds.pem
mkdir -p /usr/local/share/ca-certificates/rds /tmp/setup
awk '/-----BEGIN CERTIFICATE-----/ { n++; f=sprintf("/usr/local/share/ca-certificates/rds/rds-%03d.crt", n) } f {print > f} /-----END CERTIFICATE-----/ {close(f);f=""}' /tmp/rds.pem
update-ca-certificates >/dev/null
cd /tmp/setup
printf '%s' '__SOURCE__' | base64 -d > Program.cs
printf '%s' '__PROJECT__' | base64 -d > RuntimeUser.csproj
dotnet run --project RuntimeUser.csproj -p:NuGetAudit=false --verbosity quiet
'@
$shell = $shell.Replace('__SOURCE__', $source).Replace('__PROJECT__', $project).Replace("`r`n", "`n")
$task = @{
    family = "$StackName-runtime-user"
    executionRoleArn = $definition.executionRoleArn
    networkMode = 'awsvpc'
    requiresCompatibilities = @('FARGATE')
    cpu = '512'
    memory = '1024'
    runtimePlatform = @{ cpuArchitecture = 'X86_64'; operatingSystemFamily = 'LINUX' }
    containerDefinitions = @(@{
        name = 'setup'
        image = 'mcr.microsoft.com/dotnet/sdk:10.0'
        essential = $true
        entryPoint = @('/bin/bash', '-c')
        command = @($shell)
        secrets = @(
            @{ name = 'MigrationConnectionString'; valueFrom = "${RuntimeSecretArn}:DatabaseConnectionString::" },
            @{ name = 'RuntimeDatabasePassword'; valueFrom = "${RuntimeSecretArn}:RuntimeDatabasePassword::" }
        )
        logConfiguration = $web.logConfiguration
    })
}
# Files contain task configuration and secret ARNs only, never secret values.
$tempFile = Join-Path ([IO.Path]::GetTempPath()) ("sms-runtime-task-" + [guid]::NewGuid() + '.json')
try {
    [IO.File]::WriteAllText($tempFile, ($task | ConvertTo-Json -Depth 15), (New-Object Text.UTF8Encoding($false)))
    $registered = & aws ecs register-task-definition --cli-input-json "file://$tempFile" --region $Region --output json --no-cli-pager
    if ($LASTEXITCODE -ne 0) { throw 'Cannot register setup task.' }
    $taskArn = ($registered | ConvertFrom-Json).taskDefinition.taskDefinitionArn
    $network = @{ awsvpcConfiguration = @{ subnets = @($PublicSubnets); securityGroups = @($outputs.TaskSecurityGroupId); assignPublicIp = 'ENABLED' } }
    [IO.File]::WriteAllText($tempFile, ($network | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
    $runJson = & aws ecs run-task --cluster $outputs.ClusterName --task-definition $taskArn --launch-type FARGATE --platform-version '1.4.0' --network-configuration "file://$tempFile" --region $Region --output json --no-cli-pager
    if ($LASTEXITCODE -ne 0) { throw 'Cannot launch setup task.' }
    $run = $runJson | ConvertFrom-Json
    if ($run.failures.Count -gt 0) { throw ($run.failures | ConvertTo-Json -Depth 5) }
    Write-Output ('Setup task: ' + $run.tasks[0].taskArn)
    Write-Output ('Logs: ' + $outputs.LogGroup)
    Write-Output 'Wait for exit code 0 before changing the runtime connection string to smsapp.'
} finally {
    if (Test-Path -LiteralPath $tempFile) { Remove-Item -LiteralPath $tempFile -Force }
}
