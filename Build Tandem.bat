@echo off
rem Builds Tandem (launcher + engine) into build\Tandem, installing the build tools first if they're missing.
rem Another folder: "Build Tandem.bat" -To D:\Games\Tandem
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\tandem\build.ps1" %*
if errorlevel 1 echo. & echo The build stopped: see the messages above.
pause
