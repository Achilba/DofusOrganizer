@echo off
chcp 65001 >nul
echo.
echo  DOFUS Team Organizer - compilation
echo  ----------------------------------
dotnet --list-sdks 2>nul | findstr /B "8." >nul
if errorlevel 1 goto nosdk

dotnet publish DofusTeamOrganizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0publish"
if not exist "%~dp0publish\DofusTeamOrganizer.exe" goto failed

echo.
echo  Termine : %~dp0publish\DofusTeamOrganizer.exe
pause
exit /b 0

:nosdk
echo.
echo  Le SDK .NET 8 n'est pas installe sur ce PC.
echo  (Le "runtime" seul ne suffit pas : il faut le SDK.)
echo  1. Ouvre https://dotnet.microsoft.com/download/dotnet/8.0
echo  2. Telecharge "SDK 8.0.xxx" pour Windows x64 et installe-le
echo  3. FERME cette fenetre, puis relance build.bat
pause
exit /b 1

:failed
echo.
echo  La compilation a echoue. Copie le texte ci-dessus et envoie-le moi.
pause
exit /b 1
