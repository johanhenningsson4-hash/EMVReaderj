# Quick Fix Script for EMV Reader Project File
# Fixes the EMVReader_Refactored.cs reference issue

Write-Host "Fixing EMV Reader project file..." -ForegroundColor Yellow

$projectFile = "EMVReader.csproj"

if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: Could not find EMVReader.csproj file!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Read the current content
$content = Get-Content $projectFile -Raw

Write-Host "Looking for problematic line..." -ForegroundColor Yellow
$lines = Get-Content $projectFile
$badLine = $lines | Where-Object { $_ -like "*EMVReader_Refactored.cs*" }
if ($badLine) {
    Write-Host "FOUND PROBLEM: $($badLine.Trim())" -ForegroundColor Red
} else {
    Write-Host "No problematic reference found" -ForegroundColor Green
    Read-Host "Press Enter to exit"
    exit 0
}

# Perform the fix - replace the filename
Write-Host "Applying fix..." -ForegroundColor Green
$content = $content.Replace('EMVReader_Refactored.cs', 'EMVReader.cs')

# Add proper Form SubType if needed
if ($content -notmatch '<SubType>Form</SubType>') {
    $content = $content -replace '<Compile Include="EMVReader\.cs"\s*/>', '<Compile Include="EMVReader.cs"><SubType>Form</SubType></Compile>'
    $content = $content -replace '<Compile Include="EMVReader\.cs">', '<Compile Include="EMVReader.cs"><SubType>Form</SubType></Compile>'
}

# Save the fixed content
try {
    Set-Content -Path $projectFile -Value $content -Encoding UTF8
    Write-Host "SUCCESS: Project file updated!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to save file: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Verify the fix worked
Write-Host "Verifying fix..." -ForegroundColor Yellow
$verifyLines = Get-Content $projectFile
$goodLine = $verifyLines | Where-Object { $_ -like "*EMVReader.cs*" -and $_ -like "*Compile*" }
$stillBadLine = $verifyLines | Where-Object { $_ -like "*EMVReader_Refactored.cs*" }

if ($goodLine -and -not $stillBadLine) {
    Write-Host "VERIFIED: $($goodLine.Trim())" -ForegroundColor Green
} else {
    Write-Host "WARNING: Fix may not have worked properly" -ForegroundColor Yellow
    if ($stillBadLine) {
        Write-Host "Still found: $($stillBadLine.Trim())" -ForegroundColor Red
    }
}

# Test the build
Write-Host "Testing build..." -ForegroundColor Yellow
try {
    $buildResult = & dotnet build EMVReader.csproj --no-restore --verbosity minimal 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "BUILD SUCCESS! The fix worked!" -ForegroundColor Green
        Write-Host "You can now open Visual Studio and your project should build correctly." -ForegroundColor White
    } else {
        Write-Host "Build still has issues:" -ForegroundColor Yellow
        Write-Host $buildResult -ForegroundColor Gray
        Write-Host ""
        Write-Host "Try these steps:" -ForegroundColor White
        Write-Host "1. Open Visual Studio" -ForegroundColor Gray
        Write-Host "2. Clean Solution (Build menu -> Clean Solution)" -ForegroundColor Gray
        Write-Host "3. Rebuild Solution (Build menu -> Rebuild Solution)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Could not test build: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Please try building manually in Visual Studio" -ForegroundColor White
}

Write-Host ""
Write-Host "Fix complete! You can now open Visual Studio." -ForegroundColor Green
Read-Host "Press Enter to exit"