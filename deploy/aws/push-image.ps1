param(
    [Parameter(Mandatory)][string]$RepositoryUri,
    [string]$Region = 'eu-north-1',
    [string]$Tag = ([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
)
$ErrorActionPreference = 'Stop'
if ($RepositoryUri -notmatch '^\d{12}\.dkr\.ecr\.[a-z0-9-]+\.amazonaws\.com/[a-z0-9/_-]+$') {
    throw 'Provide the private ECR repository URI without a tag.'
}
Get-Command aws,docker -ErrorAction Stop | Out-Null
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$registry = $RepositoryUri.Split('/')[0]
$image = "{0}:{1}" -f $RepositoryUri,$Tag
Push-Location $root
try {
    & docker build --platform linux/amd64 -f SMS.Web/Dockerfile -t $image .
    if ($LASTEXITCODE -ne 0) { throw 'Container build failed.' }
    # Keep the registry token out of arguments, files and output.
    $token = & aws ecr get-login-password --region $Region
    if ($LASTEXITCODE -ne 0) { throw 'ECR authentication failed.' }
    $token | & docker login --username AWS --password-stdin $registry
    Remove-Variable token
    if ($LASTEXITCODE -ne 0) { throw 'Docker registry login failed.' }
    & docker push $image
    if ($LASTEXITCODE -ne 0) { throw 'Image push failed.' }
    Write-Output "Release image: $image"
} finally { Pop-Location }
