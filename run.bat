@echo off
setlocal

pushd "%~dp0"
if not exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    echo Error: .NET SDK not found at "%USERPROFILE%\.dotnet\dotnet.exe".
    popd
    pause
    exit /b 1
)

"%USERPROFILE%\.dotnet\dotnet.exe" run --project "MandelbrotViewer\MandelbrotViewer.csproj" -- %*
set "EXITCODE=%ERRORLEVEL%"
popd
exit /b %EXITCODE%