param(
    [string]$BaseUrl = 'http://localhost:5194',
    [string]$ExpectedIdentityHost = 'eu-north-1lohgbf9su.auth.eu-north-1.amazoncognito.com'
)
$ErrorActionPreference = 'Stop'
$base = [uri]$BaseUrl
if (-not $base.IsLoopback) { throw 'This check only permits a local test server.' }
$routes = @('/Academics/ClassTeacherAssignments', '/Academics/Notices',
    '/Academics/Prefects', '/Academics/Sdc', '/Academics/Staff',
    '/Academics/StaffNotices', '/Academics/Students', '/Academics/SubjectAssignments',
    '/Config/Classes', '/Config/Currency', '/Config/Grades', '/Config/Houses',
    '/Config/Sports', '/Config/Subjects', '/Config/Terms', '/Config/UserGroups',
    '/Config/Users', '/Config/Villages', '/Finance/Fees', '/Sports/Houses', '/Sports/Sporting')
$failed = 0
foreach ($route in $routes) {
    # A new request without a WebSession prevents accidental authenticated testing.
    $response = Invoke-WebRequest ($BaseUrl.TrimEnd('/') + $route) -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
    $status = [int]$response.StatusCode
    $location = [string]$response.Headers.Location
    $redirect = $null
    $safeRedirect = [uri]::TryCreate($location, [UriKind]::Absolute, [ref]$redirect)
    $protected = $status -in 401,403 -or ($status -eq 302 -and $safeRedirect -and
        $redirect.Scheme -eq 'https' -and $redirect.Host -eq $ExpectedIdentityHost)
    if ($protected) { Write-Output "PASS $status $route" }
    else { Write-Output "FAIL $status $route : anonymous request was not denied or challenged"; $failed++ }
}
Write-Output "RESULT: $($routes.Count - $failed) passed, $failed failed."
if ($failed) { exit 1 }
