@echo off
rem Starts the newest local build. Build it first: in Unity, Shards of Time ^> Build Executable (or the batch command in CLAUDE.md).
rem Ported from Hostile Horizon (Chastain Rush). To start through the launcher instead: "Play Shards of Time (launcher).bat".
setlocal
set "GAME=%~dp0Builds\ShardsOfTime"
if not exist "%GAME%\ShardsOfTime.exe" (
  echo Shards of Time has not been built yet. In Unity: Shards of Time ^> Build Executable.
  pause
  exit /b 1
)
start "" /D "%GAME%" "%GAME%\ShardsOfTime.exe"
