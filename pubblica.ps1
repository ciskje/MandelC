$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'

Write-Host "Pubblico MandelbrotViewer (Release, self-contained) in: $root\pubblicato"
& $dotnet publish (Join-Path $root 'MandelbrotViewer\MandelbrotViewer.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $root 'pubblicato')

if ($LASTEXITCODE -ne 0) { throw "publish fallito (exit $LASTEXITCODE)" }

$exe = Join-Path $root 'pubblicato\MandelbrotViewer.exe'
if (Test-Path $exe) {
    $ver = (Get-Item $exe).VersionInfo.ProductVersion
    Write-Host "OK: $exe (versione $ver)"
} else {
    throw "pubblicato\MandelbrotViewer.exe non trovato dopo la publish"
}
