@echo off
if not exist "%~dp0UserData\MuseDashSteamPresence\MuseDashPresenceSettings.exe" (
  echo Extract ALL files from the release ZIP into the Muse Dash game folder first.
  pause
  exit /b 1
)
start "" "%~dp0UserData\MuseDashSteamPresence\MuseDashPresenceSettings.exe"
