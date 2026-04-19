$csc = Get-ChildItem -Path "C:\Windows\Microsoft.NET\Framework64" -Filter "csc.exe" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $csc) {
    $csc = Get-ChildItem -Path "C:\Windows\Microsoft.NET\Framework" -Filter "csc.exe" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

if ($csc) {
    Write-Host "Found CSC at: $($csc.FullName)"
    $source = Join-Path $PSScriptRoot "installer.cs"
    $output = Join-Path $PSScriptRoot "ParafraseGandi-Manager.exe"
    
    $iconPath = Join-Path $PSScriptRoot "icon.ico"
    $iconArg = ""
    if (Test-Path $iconPath) {
        $iconArg = "/win32icon:$iconPath"
        Write-Host "Embedding icon: $iconPath"
    }
    # Compile as winexe with UI and Network references
    & $csc.FullName /target:winexe /out:$output $iconArg /reference:System.Windows.Forms.dll,System.dll,System.Drawing.dll,System.Net.dll $source
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Success! Generated: $output" -ForegroundColor Green
    } else {
        Write-Error "Build failed with exit code $LASTEXITCODE"
    }
} else {
    Write-Error "CSC.exe not found. .NET Framework is required to build the manager."
}
