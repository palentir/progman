# Test Script for Enhanced Program Manager

Write-Host "=== Enhanced Program Manager Test Setup ===" -ForegroundColor Green

# Check test folders
$testPath = "TestFolders"
if (Test-Path $testPath) {
    Write-Host "? Test folders created:" -ForegroundColor Green
    
    Get-ChildItem -Path $testPath -Directory | ForEach-Object {
        $folderName = $_.Name
        $shortcuts = Get-ChildItem -Path $_.FullName -Filter "*.lnk"
        Write-Host "  ?? $folderName ($($shortcuts.Count) shortcuts)" -ForegroundColor Cyan
        
        foreach ($shortcut in $shortcuts) {
            Write-Host "    • $($shortcut.BaseName)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "? Test folders not found" -ForegroundColor Red
}

Write-Host "`n=== Instructions ===" -ForegroundColor Blue
Write-Host "1. Launch Program Manager" -ForegroundColor White
Write-Host "2. Go to File ? Load Folder..." -ForegroundColor White
Write-Host "3. Select the 'TestFolders' directory" -ForegroundColor White
Write-Host "4. Dialog title should show 'NRG Speciality'" -ForegroundColor White
Write-Host "5. Office, Games, Development groups will appear" -ForegroundColor White
Write-Host "6. Check application folder for Office.ini, Games.ini, etc." -ForegroundColor White

Write-Host "`n?? Ready to test the Load Folder functionality!" -ForegroundColor Green