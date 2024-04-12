[xml]$ShiftFrameworkGlobalSettings = Get-Content ShiftFrameworkGlobalSettings.props

$ShiftFrameworkVersion = $ShiftFrameworkGlobalSettings.Project.PropertyGroup.ShiftFrameworkVersion

$TypeAuthVersion = $ShiftFrameworkGlobalSettings.Project.PropertyGroup.TypeAuthVersion

$AzureFunctionsAspNetCoreAuthorizationVersion = $ShiftFrameworkGlobalSettings.Project.PropertyGroup.AzureFunctionsAspNetCoreAuthorizationVersion

$TemplateJsonPath = "content\Framework Project\.template.config\template.json"

$TempalteJsonContent = Get-Content -Path $TemplateJsonPath | ConvertFrom-Json

$TempalteJsonContent.symbols.frameworkVersion.parameters.value = $ShiftFrameworkVersion

$TempalteJsonContent.symbols.typeAuthVersion.parameters.value = $TypeAuthVersion

$TempalteJsonContent.symbols.azureFunctionsAspNetCoreAuthorizationVersion.parameters.value = $AzureFunctionsAspNetCoreAuthorizationVersion

$TempalteJsonContent | ConvertTo-Json -Depth 100 | Set-Content -Path $TemplateJsonPath	
