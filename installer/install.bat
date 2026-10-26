@echo off
setlocal
chcp 65001 >nul
set "PS1=%~dp0install.ps1"
if not exist "%PS1%" (
	echo [ERROR] install.ps1 not found next to install.bat.
	echo         Please extract the whole zip first.
	pause
	exit /b 1
)
where powershell >nul 2>&1
if errorlevel 1 (
	echo [ERROR] Windows PowerShell not found.
	pause
	exit /b 1
)
title ColonyKit Installer
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%" %*
exit /b %ERRORLEVEL%
