@echo off
echo Running LinkSafe Kisi Synchronisation Unit Tests...
echo.

cd /d "%~dp0"

echo Building the test project...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Running tests...
dotnet test --configuration Release --verbosity normal --logger "console;verbosity=detailed"

echo.
echo Test run completed.
pause
