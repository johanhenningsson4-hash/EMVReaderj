# 🚨 IMMEDIATE FIX - Replace problematic reference in EMVReader.csproj

Write-Host "🔧 Applying immediate fix to EMVReader.csproj..." -ForegroundColor Yellow

$projectFile = "EMVReader.csproj"
$content = Get-Content $projectFile -Raw

Write-Host "🔍 Before fix - looking for problematic line:" -ForegroundColor Yellow
$lines = Get-Content $projectFile
$badLine = $lines | Where-Object { $_ -like "*EMVReader_Refactored.cs*" }
if ($badLine) {
    Write-Host "   FOUND: $($badLine.Trim())" -ForegroundColor Red
} else {
    Write-Host "   No problematic reference found" -ForegroundColor Green
}

# Direct string replacement
$content = $content.Replace('EMVReader_Refactored.cs', 'EMVReader.cs')
$content = $content -replace '<Compile Include="EMVReader\.cs"\s*/>', '<Compile Include="EMVReader.cs"><SubType>Form</SubType></Compile>'

# Save
Set-Content -Path $projectFile -Value $content -Encoding UTF8

Write-Host "✅ Fixed project file!" -ForegroundColor Green

# Verify
$verifyLines = Get-Content $projectFile
$goodLine = $verifyLines | Where-Object { $_ -like "*EMVReader.cs*" -and $_ -like "*Compile*" }
$stillBadLine = $verifyLines | Where-Object { $_ -like "*EMVReader_Refactored.cs*" }

if ($goodLine -and -not $stillBadLine) {
    Write-Host "✅ SUCCESS: $($goodLine.Trim())" -ForegroundColor Green
} else {
    Write-Host "⚠️  Issue may persist" -ForegroundColor Yellow
}

# Test build
Write-Host "🔨 Testing build..." -ForegroundColor Yellow
$result = dotnet build EMVReader.csproj --no-restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "🎉 BUILD SUCCESS!" -ForegroundColor Green
} else {
    Write-Host "❌ Build still failing:" -ForegroundColor Red
    Write-Host $result -ForegroundColor Yellow
}