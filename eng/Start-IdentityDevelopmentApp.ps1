param([string]$ArtifactsPath = (Join-Path ([IO.Path]::GetTempPath()) 'ShiftIdentityDevelopmentApp'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../content/Framework Project/StockPlusPlus.API/StockPlusPlus.API.csproj'
# Separate artifacts keep the owned review process from locking normal IDE build outputs.
& dotnet build $project --configuration Debug --artifacts-path $ArtifactsPath -p:IdentityDevelopmentApp=true -p:IdentityAdmissionPreview=false
if ($LASTEXITCODE -ne 0) { throw 'The isolated development app could not build.' }
& dotnet run --project $project --configuration Debug --artifacts-path $ArtifactsPath --no-build --no-restore --no-launch-profile -p:IdentityDevelopmentApp=true -p:IdentityAdmissionPreview=false -- --identity-development
if ($LASTEXITCODE -ne 0) { throw 'The isolated development app could not run.' }
