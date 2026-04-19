$content = Get-Content -Raw "installer.cs"
$new_ui = Get-Content -Raw "new_ui.cs"

$start = $content.IndexOf("        // --- MD3 DESIGN TOKENS")
$end = $content.IndexOf("        // ======== CORE LOGIC ========")

if ($start -ge 0 -and $end -ge 0) {
    $before = $content.Substring(0, $start)
    $after = $content.Substring($end)
    $updated = $before + $new_ui + "`r`n" + $after
    [System.IO.File]::WriteAllText("installer.cs", $updated, [System.Text.Encoding]::UTF8)
    Write-Host "Replaced successfully!"
} else {
    Write-Host "Markers not found."
}
