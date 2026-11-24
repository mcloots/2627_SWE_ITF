[CmdletBinding()]
param(
    [switch]$PrepareOnly,
    [switch]$Restart
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$apiProject = Join-Path $repositoryRoot 'backend/src/ITFPulse.Api'
$containerName = 'itfpulse-k6-db'

# Windows locks the API's output DLLs while it is running. Check before building
# or migrating, and only stop this repository's Release API when requested.
$apiExecutable = Join-Path $apiProject 'bin/Release/net10.0/ITFPulse.Api.exe'
$runningApis = @(Get-Process -Name ITFPulse.Api -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $apiExecutable })
foreach ($runningApi in $runningApis) {
    if (!$Restart) {
        throw "The workshop API is already running (PID $($runningApi.Id)). Stop it with Ctrl+C in its terminal, or rerun this script with -Restart before rebuilding/migrating."
    }
    Stop-Process -InputObject $runningApi -ErrorAction Stop
    if (!$runningApi.WaitForExit(10000)) { throw 'The previous workshop API did not stop.' }
}

Get-Command docker, dotnet -ErrorAction Stop | Out-Null
docker info --format '{{.ServerVersion}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Start Docker Desktop, then run this script again.' }

$containerId = docker ps -a --filter "name=^/$containerName`$" --format '{{.ID}}'
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect Docker containers.' }
if ($containerId) {
    $container = (docker inspect $containerName | ConvertFrom-Json)[0]
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the workshop database.' }
    $binding = $container.HostConfig.PortBindings.'5432/tcp'
    if ($container.Config.Env -notcontains 'POSTGRES_USER=workshop' -or
        $container.Config.Env -notcontains 'POSTGRES_PASSWORD=workshop' -or
        $container.Config.Env -notcontains 'POSTGRES_DB=itfpulse_k6' -or
        $binding.Count -ne 1 -or $binding[0].HostIp -ne '127.0.0.1' -or
        $binding[0].HostPort -ne '55432') {
        throw "Existing $containerName has different settings. Resolve the name/port conflict before continuing."
    }
    docker start $containerName | Out-Null
} else {
    docker run --name $containerName -e POSTGRES_USER=workshop -e POSTGRES_PASSWORD=workshop `
        -e POSTGRES_DB=itfpulse_k6 -p 127.0.0.1:55432:5432 -d postgres:18 | Out-Null
}
if ($LASTEXITCODE -ne 0) { throw 'Could not start the workshop database.' }

$ready = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    docker exec $containerName pg_isready -h 127.0.0.1 -U workshop -d itfpulse_k6 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 1
}
if (!$ready) { throw "PostgreSQL did not become ready. Inspect: docker logs $containerName" }

# Explicit settings override any local .env defaults in the API.
$env:ITFPULSE_ConnectionStrings__ITFPulse = 'Host=localhost;Port=55432;Database=itfpulse_k6;Username=workshop;Password=workshop'
$env:ITFPULSE_Hosting__HttpsTerminatedAtProxy = 'true'
$env:ITFPULSE_Cors__AllowedOrigins__0 = 'http://localhost:6510'

dotnet run --project $apiProject --configuration Release --no-launch-profile -- --migrate
if ($LASTEXITCODE -ne 0) { throw 'Workshop database migration failed.' }
if ($PrepareOnly) { return }

Write-Host 'Workshop API: http://localhost:5212. Leave this terminal open; Ctrl+C stops the API.'
dotnet run --project $apiProject --configuration Release --no-build --no-launch-profile -- --urls http://localhost:5212
if ($LASTEXITCODE -ne 0) { throw 'Workshop API stopped with an error.' }
