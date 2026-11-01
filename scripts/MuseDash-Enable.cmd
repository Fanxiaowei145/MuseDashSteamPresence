@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0UserData\MuseDashSteamPresence\enable.ps1" -GamePath "%~dp0."
pause
