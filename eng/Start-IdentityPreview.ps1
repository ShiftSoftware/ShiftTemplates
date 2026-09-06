$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../content/Framework Project/StockPlusPlus.API/StockPlusPlus.API.csproj'
# Deliberately ignore launch profiles. The selected host reads no appsettings or user secrets,
# binds a fresh loopback port and owns its synthetic SQL database through the shared fixture.
& dotnet build $project --configuration Debug -p:IdentityAdmissionPreview=true
if ($LASTEXITCODE -ne 0) { throw 'The local admission preview could not build.' }
& dotnet run --project $project --configuration Debug --no-build --no-restore --no-launch-profile -p:IdentityAdmissionPreview=true -- --identity-preview
if ($LASTEXITCODE -ne 0) { throw 'The local admission preview could not run.' }
