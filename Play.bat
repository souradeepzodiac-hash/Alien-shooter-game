@echo off
setlocal
cd /d "%~dp0"
if exist "publish\abyss-fx\VoidHunter.exe" (
  start "" "publish\abyss-fx\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-kids\VoidHunter.exe" (
  start "" "publish\abyss-kids\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-stable\VoidHunter.exe" (
  start "" "publish\abyss-stable\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-aim\VoidHunter.exe" (
  start "" "publish\abyss-aim\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-look\VoidHunter.exe" (
  start "" "publish\abyss-look\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-open\VoidHunter.exe" (
  start "" "publish\abyss-open\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-keys\VoidHunter.exe" (
  start "" "publish\abyss-keys\VoidHunter.exe"
  exit /b 0
)
if exist "publish\abyss-ctrl\VoidHunter.exe" (
  start "" "publish\abyss-ctrl\VoidHunter.exe"
  exit /b 0
)
if exist "publish\win-x64-fix\VoidHunter.exe" (
  start "" "publish\win-x64-fix\VoidHunter.exe"
  exit /b 0
)
if exist "publish\win-x64-new\VoidHunter.exe" (
  start "" "publish\win-x64-new\VoidHunter.exe"
  exit /b 0
)
if exist "publish\win-x64\VoidHunter.exe" (
  start "" "publish\win-x64\VoidHunter.exe"
  exit /b 0
)
if exist "bin\Release\net10.0\VoidHunter.exe" (
  start "" "bin\Release\net10.0\VoidHunter.exe"
  exit /b 0
)
if exist "bin\Debug\net10.0\VoidHunter.exe" (
  start "" "bin\Debug\net10.0\VoidHunter.exe"
  exit /b 0
)
echo Build the game first:
echo   "C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 --self-contained true -o publish\win-x64
pause
