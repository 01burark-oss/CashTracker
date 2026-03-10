@echo off
setlocal

set "EXE=%~dp0CashTracker.LicenseAdmin\bin\Debug\net8.0-windows\CashTracker.LicenseAdmin.exe"

if not exist "%EXE%" (
  echo CashTracker License Admin bulunamadi:
  echo %EXE%
  pause
  exit /b 1
)

start "" "%EXE%"
