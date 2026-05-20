# Generate a self-signed dev cert covering the Builder's *.local hostnames + localhost
# and trust it for the current Windows user. Idempotent.
#
# The Builder maps each TestProject-* config to its own hostname (int-srv.local,
# int-wasm.local, ext-srv.local, ext-wasm.local) so cookies stay isolated per app.
# `dotnet dev-certs https` only covers `localhost`, so we generate a multi-SAN cert
# here. Kestrel loads the PFX via env vars set in each project's launchSettings.json.

$ErrorActionPreference = 'Stop'

$dnsNames = @(
    'localhost',
    'int-srv.local',
    'int-wasm.local',
    'ext-srv.local',
    'ext-wasm.local',
    'web.local'
)

$friendlyName = 'ShiftTemplates Dev Cert'
$pfxPassword  = 'devcert'
$pfxPath      = Join-Path $PSScriptRoot 'dev.pfx'

$existing = Get-ChildItem -Path Cert:\CurrentUser\My |
    Where-Object { $_.FriendlyName -eq $friendlyName }

if ($existing -and (Test-Path $pfxPath)) {
    Write-Host "Dev cert already present (PFX: $pfxPath). Delete both the PFX and the cert"
    Write-Host "in Cert:\CurrentUser\My + Cert:\CurrentUser\Root to regenerate."
    return
}

# Clean up partial state from previous runs
if ($existing) {
    $existing | ForEach-Object { Remove-Item -Path "Cert:\CurrentUser\My\$($_.Thumbprint)" -Force }
}

$cert = New-SelfSignedCertificate `
    -DnsName $dnsNames `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -FriendlyName $friendlyName `
    -KeyExportPolicy Exportable `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.1')

$securePassword = ConvertTo-SecureString -String $pfxPassword -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null

# Trust it: Chrome on Windows reads CurrentUser\Root (no admin required).
Import-PfxCertificate `
    -FilePath $pfxPath `
    -CertStoreLocation 'Cert:\CurrentUser\Root' `
    -Password $securePassword | Out-Null

Write-Host ""
Write-Host "Created dev cert and trusted it in CurrentUser\Root."
Write-Host "  PFX:       $pfxPath"
Write-Host "  Password:  $pfxPassword"
Write-Host "  Hostnames: $($dnsNames -join ', ')"
Write-Host ""
Write-Host "Make sure your hosts file has the .local entries pointing at 127.0.0.1."
Write-Host "Restart the browser if it was already open."
