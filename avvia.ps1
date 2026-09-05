$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "MandelbrotViewer"
$StandaloneExe = Join-Path $ScriptDir "pubblicato\MandelbrotViewer.exe"
$Exe = Join-Path $ProjectDir "bin\Debug\net8.0-windows\MandelbrotViewer.exe"

$UserDotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (Test-Path -LiteralPath $UserDotnet) {
    $Dotnet = $UserDotnet
} else {
    $Dotnet = "dotnet"
}

if (Test-Path -LiteralPath $StandaloneExe) {
    Write-Host "Avvio $StandaloneExe ..."
    Start-Process -FilePath $StandaloneExe
} elseif (Test-Path -LiteralPath $Exe) {
    Write-Host "Avvio $Exe ..."
    Start-Process -FilePath $Exe
} else {
    Write-Host "Eseguibile non trovato, compilo e avvio con dotnet..."
    & $Dotnet run --project $ProjectDir
}