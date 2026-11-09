@echo off
rem Starts the game through the launcher, as a player would (Round 1). Uses a local launcher install of the newest build
rem (Builds\LauncherLocal, made by Tools\make_local_launcher.py; remade here whenever the build is newer than it).
rem The launcher checks the real catalog (shards-of-time-releases): a newer published release replaces this local copy.
setlocal
set "ROOT=%~dp0"
set "INST=%ROOT%Builds\LauncherLocal\ShardsOfTime"
if not exist "%ROOT%Builds\ShardsOfTime\ShardsOfTime.exe" (
  echo Shards of Time has not been built yet. In Unity: Shards of Time ^> Build Executable.
  pause
  exit /b 1
)
set "REMAKE="
if not exist "%INST%\made.txt" set "REMAKE=1"
if exist "%INST%\made.txt" (
  for /f %%i in ('powershell -NoProfile -Command "if ((Get-Item '%ROOT%Builds\ShardsOfTime\ShardsOfTime_Data\globalgamemanagers').LastWriteTime -gt (Get-Item '%INST%\made.txt').LastWriteTime) {1} else {0}"') do if "%%i"=="1" set "REMAKE=1"
)
if defined REMAKE python "%ROOT%Tools\make_local_launcher.py" || (pause & exit /b 1)
start "" /D "%INST%" "%INST%\ShardsOfTimeLauncher.exe"
