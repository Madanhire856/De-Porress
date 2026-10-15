param([string]$BaseUrl = 'http://localhost:5198')
$ErrorActionPreference = 'Stop'
if (-not ([uri]$BaseUrl).IsLoopback) { throw 'Run this only against the local UAT server.' }
$checks = 0
function Check([bool]$condition, [string]$name) {
    if (-not $condition) { throw "FAIL: $name" }
    $script:checks++
    Write-Output "PASS: $name"
}
foreach ($route in @('/health', '/', '/Notices')) {
    $response = Invoke-WebRequest ($BaseUrl + $route) -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
    Check ($response.StatusCode -eq 200) "$route returns 200"
}
$response = Invoke-WebRequest ($BaseUrl + '/Identity/Account/Logout') -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
Check ($response.StatusCode -eq 405) 'GET cannot sign a user out'
$response = Invoke-WebRequest ($BaseUrl + '/Identity/Account/Logout') -Method Post -Body @{} -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
Check ($response.StatusCode -eq 400) 'Logout POST requires antiforgery'
# Loopback is trusted by the ASP.NET forwarded-header middleware. No user tokens
# are generated or exchanged by this redirect-only probe.
$response = Invoke-WebRequest ($BaseUrl + '/Identity/Account/StaffLogin') -Headers @{
    'X-Forwarded-Proto' = 'https'; 'Host' = 'localhost:7246'
} -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
Check ($response.StatusCode -eq 302) 'Staff login issues authorization redirect'
$redirect = [uri][string]$response.Headers.Location
Check ($redirect.Scheme -eq 'https' -and $redirect.Host -eq 'eu-north-1lohgbf9su.auth.eu-north-1.amazoncognito.com') 'Redirect goes only to configured Cognito'
$query = @{}
foreach ($pair in $redirect.Query.TrimStart('?').Split('&')) {
    $parts = $pair.Split('=', 2)
    $query[[System.Net.WebUtility]::UrlDecode($parts[0])] = [System.Net.WebUtility]::UrlDecode($parts[1])
}
Check ($query['client_id'] -eq '6ulsn0evv7q8qmoeo69al441sg') 'Correct app client'
Check ($query['response_type'] -eq 'code') 'Authorization code flow'
Check ($query['code_challenge_method'] -eq 'S256' -and -not [string]::IsNullOrWhiteSpace($query['code_challenge'])) 'PKCE challenge present'
Check (-not [string]::IsNullOrWhiteSpace($query['state']) -and -not [string]::IsNullOrWhiteSpace($query['nonce'])) 'State and nonce present'
Check ($query['redirect_uri'] -eq 'https://localhost:7246/signin-oidc') 'Trusted proxy preserves registered HTTPS callback'
$scopes = $query['scope'].Split(' ')
Check (@('openid','email','profile').Where({$_ -notin $scopes}).Count -eq 0) 'Required identity scopes present'
Check ('phone' -notin $scopes) 'Unneeded phone scope omitted'
Write-Output "RESULT: $checks AWS runtime checks passed. No user login or token exchange attempted."
