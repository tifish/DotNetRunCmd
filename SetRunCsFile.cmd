@echo off
setlocal enabledelayedexpansion
cd "%~dp0"

"C:\Program Files\dotnet\dotnet.exe" run %~n0.cs
if errorlevel 1 pause

endlocal
