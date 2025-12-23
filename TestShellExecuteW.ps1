# Test Script for ShellExecuteW Shortcut Properties

Write-Host "=== ShellExecuteW Shortcut Properties Test ===" -ForegroundColor Green

Write-Host "`n=== Updated Implementation ===" -ForegroundColor Blue
Write-Host "? Replaced rundll32 approach with ShellExecuteW API" -ForegroundColor White
Write-Host "? Uses native Windows 'properties' verb" -ForegroundColor White
Write-Host "? More reliable than rundll32 method" -ForegroundColor White

Write-Host "`n=== Technical Details ===" -ForegroundColor Yellow
Write-Host "?? API Call:" -ForegroundColor Cyan
Write-Host "   ShellExecuteW(hwnd, ""properties"", ""path.lnk"", null, null, SW_SHOW)" -ForegroundColor Gray

Write-Host "`n?? Parameters:" -ForegroundColor Cyan
Write-Host "   • hwnd: Parent window handle (this.Handle)" -ForegroundColor White
Write-Host "   • lpOperation: ""properties"" (opens properties dialog)" -ForegroundColor White
Write-Host "   • lpFile: Full path to .lnk file" -ForegroundColor White
Write-Host "   • lpParameters: null (not needed for properties)" -ForegroundColor White
Write-Host "   • lpDirectory: null (uses default)" -ForegroundColor White
Write-Host "   • nShowCmd: SW_SHOW (5) - shows the dialog" -ForegroundColor White

Write-Host "`n=== Implementation Features ===" -ForegroundColor Magenta
Write-Host "?? Error Handling:" -ForegroundColor Cyan
Write-Host "   • Checks return value (>32 = success)" -ForegroundColor White
Write-Host "   • Fallback to custom form if API fails" -ForegroundColor White
Write-Host "   • User-friendly error messages" -ForegroundColor White

Write-Host "`n?? Auto-Refresh:" -ForegroundColor Cyan
Write-Host "   • Timer-based refresh (500ms delay)" -ForegroundColor White
Write-Host "   • Updates display after properties change" -ForegroundColor White
Write-Host "   • Thread-safe using BeginInvoke" -ForegroundColor White

Write-Host "`n=== Advantages over rundll32 ===" -ForegroundColor Green
Write-Host "• More direct API call - no external process" -ForegroundColor White
Write-Host "• Better error handling and reporting" -ForegroundColor White
Write-Host "• Consistent with Windows Shell operations" -ForegroundColor White
Write-Host "• Less likely to be blocked by security software" -ForegroundColor White
Write-Host "• Standard approach for Shell operations" -ForegroundColor White

Write-Host "`n=== Testing Instructions ===" -ForegroundColor Blue
Write-Host "1. Launch Program Manager" -ForegroundColor White
Write-Host "2. Navigate to a group with shortcuts" -ForegroundColor White
Write-Host "3. Right-click on a shortcut ? 'Properties'" -ForegroundColor White
Write-Host "4. Should open Windows shortcut properties dialog" -ForegroundColor White
Write-Host "5. Make a change (e.g., modify description)" -ForegroundColor White
Write-Host "6. Click OK to close the dialog" -ForegroundColor White
Write-Host "7. Verify the group refreshes automatically" -ForegroundColor White
Write-Host "8. Test with File menu ? Properties (when shortcut selected)" -ForegroundColor White

Write-Host "`n=== Expected Results ===" -ForegroundColor Red
Write-Host "? Properties dialog opens immediately" -ForegroundColor White
Write-Host "? All standard shortcut properties available" -ForegroundColor White
Write-Host "? Changes save when OK is clicked" -ForegroundColor White
Write-Host "? Group display refreshes automatically" -ForegroundColor White
Write-Host "? No error messages or console windows" -ForegroundColor White
Write-Host "? Consistent behavior from both menu locations" -ForegroundColor White

Write-Host "`n=== Fallback Behavior ===" -ForegroundColor Yellow
Write-Host "If ShellExecuteW fails:" -ForegroundColor White
Write-Host "• Shows error message with details" -ForegroundColor White
Write-Host "• Automatically opens custom properties form" -ForegroundColor White
Write-Host "• User can still edit shortcut properties" -ForegroundColor White
Write-Host "• Maintains full functionality" -ForegroundColor White

Write-Host "`n?? Ready to test ShellExecuteW shortcut properties!" -ForegroundColor Green