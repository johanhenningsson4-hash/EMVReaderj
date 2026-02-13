@echo off
echo 🔧 Fixing EMVReader.csproj - Replace EMVReader_Refactored.cs with EMVReader.cs
echo.

REM Create a backup
copy EMVReader.csproj EMVReader.csproj.backup

REM Use PowerShell to do the replacement
powershell -Command "(Get-Content 'EMVReader.csproj') -replace 'EMVReader_Refactored\.cs', 'EMVReader.cs' | Set-Content 'EMVReader.csproj'"

echo ✅ Project file updated!
echo.
echo 🔨 Testing build...
dotnet build EMVReader.csproj --no-restore

if %ERRORLEVEL% EQU 0 (
    echo.
    echo 🎉 BUILD SUCCESS! 
    echo ✅ All build errors fixed!
) else (
    echo.
    echo ⚠️ Build still has issues. You may need to:
    echo   1. Close Visual Studio completely
    echo   2. Reopen the solution  
    echo   3. Clean and rebuild
)

pause