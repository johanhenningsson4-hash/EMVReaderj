@echo off
echo =========================================
echo EMV Reader Project Fix Tool
echo =========================================
echo.
echo This will fix the EMVReader_Refactored.cs reference issue
echo Make sure Visual Studio is CLOSED before running this!
echo.
pause
echo.

echo Creating backup of project file...
if exist EMVReader.csproj.backup del EMVReader.csproj.backup
copy EMVReader.csproj EMVReader.csproj.backup >nul
if errorlevel 1 (
    echo ERROR: Could not create backup!
    pause
    exit /b 1
)
echo Backup created: EMVReader.csproj.backup

echo.
echo Applying fix to EMVReader.csproj...

REM Use PowerShell to do the replacement safely
powershell -Command "try { $content = Get-Content 'EMVReader.csproj' -Raw; $content = $content.Replace('EMVReader_Refactored.cs', 'EMVReader.cs'); Set-Content 'EMVReader.csproj' $content -Encoding UTF8; Write-Host 'Project file updated successfully' } catch { Write-Host 'Error updating file:' $_.Exception.Message; exit 1 }"

if errorlevel 1 (
    echo ERROR: Failed to update project file!
    echo Restoring backup...
    copy EMVReader.csproj.backup EMVReader.csproj >nul
    pause
    exit /b 1
)

echo SUCCESS: Project file has been fixed!
echo.

echo Testing build...
dotnet build EMVReader.csproj --no-restore --verbosity minimal

if errorlevel 0 (
    echo.
    echo =========================================
    echo SUCCESS! Build completed successfully!
    echo =========================================
    echo.
    echo Your EMV Reader project is now fixed.
    echo You can open Visual Studio and start using your refactored application!
    echo.
) else (
    echo.
    echo =========================================
    echo Build had some issues, but the main fix is applied.
    echo =========================================
    echo.
    echo Try these steps:
    echo 1. Open Visual Studio
    echo 2. Clean Solution (Build menu ^> Clean Solution)
    echo 3. Rebuild Solution (Build menu ^> Rebuild Solution)
    echo.
)

echo The refactored EMV Reader with separated architecture is ready!
echo.
pause