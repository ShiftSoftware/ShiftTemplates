#requires -Version 7
# Generates new projects from a packed template and builds them against packed packages. The release pipeline runs this
# on what it has just packed, so that a template that gives a broken project stops the release before it is published.
#
# The projects are generated outside the repository, with the template in a hive of its own and the packages in a folder
# of their own, so nothing under test reaches the machine's installed templates or its NuGet cache.
#
# On a developer machine: pack the framework packages and the template into a folder, then run
#   eng/Test-NewProjects.ps1 -PackagesPath <folder> -TemplatePackage <folder>/ShiftSoftware.ShiftTemplates.*.nupkg
# Pack from a clean export with Linux line endings (git -c core.autocrlf=false -c core.eol=lf archive HEAD). The release
# is packed on Linux, and with Windows line endings the template engine can remove less of an #if region than it should.
param(
    # The folders with the packages under test, searched with their subfolders. A package found here is never taken from
    # nuget.org; every other package is.
    [Parameter(Mandatory)][string[]]$PackagesPath,
    # The template package. A wildcard is fine as long as it matches one file.
    [Parameter(Mandatory)][string]$TemplatePackage,
    # The projects go into a folder of their own in this one. It is removed, unless a project fails.
    [string]$WorkPath = [IO.Path]::GetTempPath()
)
$ErrorActionPreference = 'Stop'

$template = @(Get-Item -Path $TemplatePackage)
if ($template.Count -ne 1) { throw "'$TemplatePackage' must be one template package, and it matches $($template.Count)." }
$packages = @(Get-ChildItem -LiteralPath $PackagesPath -Recurse -File -Filter '*.nupkg')
if ($packages.Count -eq 0) { throw "There are no packages in '$($PackagesPath -join "', '")'." }

# Each package ID is mapped to its source, so that a version that is also on nuget.org is still the one under test.
$folders = foreach ($group in $packages | Group-Object DirectoryName) {
    @{ Path = [Security.SecurityElement]::Escape($group.Name); Ids = $group.Group.BaseName -replace '\.\d+\.\d+.*$' | Sort-Object -Unique }
}
$nugetConfig = @(
    '<?xml version="1.0" encoding="utf-8"?>'
    '<configuration>'
    '  <packageSources>'
    '    <clear />'
    foreach ($folder in $folders) { "    <add key=""$($folder.Path)"" value=""$($folder.Path)"" />" }
    '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
    '  </packageSources>'
    '  <disabledPackageSources>'
    '    <clear />'
    '  </disabledPackageSources>'
    '  <packageSourceMapping>'
    '    <clear />'
    foreach ($folder in $folders) {
        "    <packageSource key=""$($folder.Path)"">"
        foreach ($id in $folder.Ids) { "      <package pattern=""$id"" />" }
        '    </packageSource>'
    }
    '    <packageSource key="nuget.org">'
    '      <package pattern="*" />'
    '    </packageSource>'
    '  </packageSourceMapping>'
    '</configuration>'
)

$kinds = foreach ($identity in 'Internal', 'External') {
    @{ Name = "${identity}Full"; Identity = $identity; Sample = 'true'; FunctionsAndTests = 'true' }
    @{ Name = "${identity}NoSample"; Identity = $identity; Sample = 'false'; FunctionsAndTests = 'true' }
    @{ Name = "${identity}SampleOnly"; Identity = $identity; Sample = 'true'; FunctionsAndTests = 'false' }
}

$work = (New-Item -ItemType Directory -Path (Join-Path $WorkPath "NewProjects-$([Guid]::NewGuid().ToString('N').Substring(0, 8))")).FullName
$hive = Join-Path $work 'hive'
$restored = Join-Path $work 'packages'
Write-Host "$($template[0].Name), with $($packages.Count) packages under test. Working in $work."

& dotnet new install $template[0].FullName '--debug:custom-hive' $hive | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'The template package could not be installed.' }

$problems = @(foreach ($kind in $kinds) {
    $name = $kind.Name
    $project = Join-Path $work $name
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $options = @('--includeSampleApp', $kind.Sample, '--shiftIdentityHostingType', $kind.Identity, '--addFunctions', $kind.FunctionsAndTests, '--addTest', $kind.FunctionsAndTests)
    Write-Host "=== $name ($options)"

    & dotnet new shift -n $name -o $project @options '--debug:custom-hive' $hive | Out-Host
    if ($LASTEXITCODE -ne 0) { "${name}: the project could not be generated."; continue }

    $files = Get-ChildItem -LiteralPath $project -Recurse -File -Force
    $solution = Get-Content -LiteralPath (Join-Path $project "$name.sln") -Raw
    $found = @(
        $files | Where-Object { $_.Extension -eq '.cs' -and [string]::IsNullOrWhiteSpace([IO.File]::ReadAllText($_.FullName)) } | ForEach-Object {
            "${name}: $([IO.Path]::GetRelativePath($project, $_.FullName)) is empty."
        }
        # As C# writes it, and in the comments that the other file types need.
        $files | Select-String -CaseSensitive -Pattern '^\s*(<!--|@\*|//|/\*)?\s*#(if|elif|elseif|else|endif)\b' | ForEach-Object {
            "${name}: $([IO.Path]::GetRelativePath($project, $_.Path)) line $($_.LineNumber) has a leftover '$($_.Line.Trim())'."
        }
        # What is not in the solution is not built below.
        $files | Where-Object { $_.Extension -eq '.csproj' -and -not $solution.Contains($_.Name) } | ForEach-Object {
            "${name}: $($_.Name) is not in the solution."
        }
    )

    Set-Content -LiteralPath (Join-Path $project 'nuget.config') -Value $nugetConfig
    # NU1603: a version that is asked for does not exist, and NuGet takes a later one and only warns. Of TypeAuth, whose
    # numbering started over, it would take one from 2023.
    & dotnet build (Join-Path $project "$name.sln") "-p:RestorePackagesPath=$restored" '-warnaserror:NU1603' '-nodeReuse:false' -nologo -v:q '-clp:ErrorsOnly;NoSummary' | Out-Host
    if ($LASTEXITCODE -ne 0) { $found += "${name}: the solution does not build." }

    $found
    $result = if ($found.Count -eq 0) { 'passed' } else { 'failed' }
    Write-Host "$name $result in $([int]$watch.Elapsed.TotalSeconds) seconds."
    # One project on disk at a time. All of them together could be too large for a hosted agent.
    if ($found.Count -eq 0) { Remove-Item -LiteralPath $project -Recurse -Force -ErrorAction SilentlyContinue }
})

if ($problems.Count -gt 0) {
    # The pipeline's summary page lists what is logged as an issue.
    $prefix = if ($env:TF_BUILD) { '##vso[task.logissue type=error]' } else { '' }
    foreach ($problem in $problems) { Write-Host "$prefix$problem" }
    throw "The template gives broken projects. The ones that failed are in $work."
}
Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "All $($kinds.Count) kinds of new project build."
