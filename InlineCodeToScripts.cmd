@echo off
setlocal enabledelayedexpansion

"%ProgramFiles%\dotnet\dotnet.exe" --version | findstr /r "^10\." >nul 2>nul
if %errorlevel% neq 0 (
    winget install Microsoft.DotNET.SDK.10
)

"%ProgramFiles%\dotnet\dotnet.exe" run "%~dpn0.cs" %*

endlocal
