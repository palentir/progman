# Test Script for Profile System

Write-Host "=== Program Manager Profile System Test ===" -ForegroundColor Green

# Check if test structure exists
if (Test-Path "TestFolders") {
    Write-Host "? Test folder structure found:" -ForegroundColor Green
    
    # Show root .lnk files
    $rootFiles = Get-ChildItem -Path "TestFolders" -Filter "*.lnk"
    Write-Host "  Root .lnk files (will go to Main group):" -ForegroundColor Yellow
    foreach ($file in $rootFiles) {
        Write-Host "    • $($file.BaseName)" -ForegroundColor Cyan
    }
    
    # Show subfolders
    $subfolders = Get-ChildItem -Path "TestFolders" -Directory
    Write-Host "  Subfolders (will become groups):" -ForegroundColor Yellow
    foreach ($folder in $subfolders) {
        $shortcuts = Get-ChildItem -Path $folder.FullName -Filter "*.lnk" -Recurse
        Write-Host "    ?? $($folder.Name) ($($shortcuts.Count) shortcuts)" -ForegroundColor Cyan
    }
}

Write-Host "`n=== Test Instructions ===" -ForegroundColor Blue
Write-Host "1. Launch Program Manager" -ForegroundColor White
Write-Host "2. Notice 'Profile' menu between File and Windows" -ForegroundColor White
Write-Host "3. Current profile should be 'Main' (checked)" -ForegroundColor White
Write-Host "4. Go to Profile ? Add Profile..." -ForegroundColor White
Write-Host "5. Dialog shows 'NRG Speciality' title" -ForegroundColor White
Write-Host "6. Select TestFolders directory" -ForegroundColor White
Write-Host "7. Name it 'TestProfile'" -ForegroundColor White
Write-Host "8. See groups: Main (root files), Office, Games, Development" -ForegroundColor White
Write-Host "9. Check app folder for .ini files (Office.ini, Games.ini, etc.)" -ForegroundColor White
Write-Host "10. Window title shows 'Program Manager - TestProfile'" -ForegroundColor White

Write-Host "`n=== Expected Results ===" -ForegroundColor Magenta
Write-Host "• Main group contains root .lnk files" -ForegroundColor White
Write-Host "• Office, Games, Development groups from subfolders" -ForegroundColor White
Write-Host "• Settings stored in Progman.ini" -ForegroundColor White
Write-Host "• Profile menu shows TestProfile (checked)" -ForegroundColor White
Write-Host "• Delete Profile enabled for TestProfile" -ForegroundColor White

Write-Host "`n?? Ready to test the Profile system!" -ForegroundColor Green