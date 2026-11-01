@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0UserData\MuseDashSteamPresence\disable.ps1" -GamePath "%~dp0."
pause
