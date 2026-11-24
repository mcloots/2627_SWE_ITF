[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5212',
    [string]$K6Path = 'k6'
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$uri = [uri]$BaseUrl
if (!$uri.IsLoopback -or $uri.Scheme -ne 'http' -or $uri.AbsolutePath -ne '/') {
    throw 'Use a local HTTP API with a disposable workshop database. BaseUrl must not include /api.'
}
Get-Command $K6Path -ErrorAction Stop | Out-Null
$results = Join-Path $PSScriptRoot 'results'
New-Item -ItemType Directory -Force $results | Out-Null

$health = Invoke-RestMethod "$BaseUrl/api/health"
if ($health.status -notlike 'Healthy*') { throw 'Health check failed.' }

$authorId = '11111111-1111-4111-8111-111111111111'
$content = "k6-workshop smoke fixture $([guid]::NewGuid())"
$payload = @{ authorId = $authorId; content = $content } | ConvertTo-Json
$response = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$BaseUrl/api/posts" `
    -ContentType 'application/json' -Body $payload
$created = $response.Content | ConvertFrom-Json
if ($response.StatusCode -ne 201 -or $response.Headers.Location -ne "/api/posts/$($created.postId)") {
    throw 'Publishing did not return 201 with the expected Location.'
}
$saved = Invoke-RestMethod "$BaseUrl/api/posts/$($created.postId)"
if ($saved.postId -ne $created.postId -or $saved.authorId -ne $authorId -or $saved.content -ne $content) {
    throw 'Reading the fixture did not return the persisted post.'
}

$unknownStatus = $null
try { Invoke-WebRequest -UseBasicParsing "$BaseUrl/api/posts/$([guid]::NewGuid())" | Out-Null }
catch {
    if (!$_.Exception.Response) { throw }
    $unknownStatus = [int]$_.Exception.Response.StatusCode
}
if ($unknownStatus -ne 404) { throw 'An unknown post must return 404.' }

$cors = Invoke-WebRequest -UseBasicParsing -Method Options -Uri "$BaseUrl/api/posts" -Headers @{
    Origin = 'http://localhost:6510'
    'Access-Control-Request-Method' = 'POST'
    'Access-Control-Request-Headers' = 'content-type'
}
if ($cors.Headers['Access-Control-Allow-Origin'] -ne 'http://localhost:6510') {
    throw 'CORS does not allow the workshop frontend.'
}

# Keep smoke runs unattended, even after using the dashboard in this terminal.
$dashboardSettings = @{}
foreach ($name in @('K6_WEB_DASHBOARD', 'K6_WEB_DASHBOARD_EXPORT')) {
    $dashboardSettings[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
    [Environment]::SetEnvironmentVariable($name, $null, 'Process')
}
$env:K6_WEB_DASHBOARD = 'false'
try {
    foreach ($target in @('health', 'posts', 'read')) {
        & $K6Path run --quiet --include-system-env-vars=false `
            -e "BASE_URL=$BaseUrl" -e "TARGET=$target" -e ALLOW_WRITES=true `
            -e "AUTHOR_ID=$authorId" -e "POST_ID=$($created.postId)" -e "EXPECTED_CONTENT=$content" `
            -e RATE=1 -e DURATION=5s --summary-export (Join-Path $results "smoke-$target.json") `
            (Join-Path $PSScriptRoot 'project-load.js')
        if ($LASTEXITCODE -ne 0) { throw "k6 $target smoke test failed (exit $LASTEXITCODE)." }
    }
} finally {
    foreach ($name in $dashboardSettings.Keys) {
        [Environment]::SetEnvironmentVariable($name, $dashboardSettings[$name], 'Process')
    }
}
Write-Host "Smoke tests passed: health, persisted POST/GET, unknown ID, CORS, and all three k6 targets. Results: $results"
