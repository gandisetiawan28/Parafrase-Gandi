$text = Get-Content -Raw "top.cs"
$text = $text.Replace("private static Color primaryFixed = ColorTranslator.FromHtml(`"#d8e2ff`");", "private static Color primaryFixed = ColorTranslator.FromHtml(`"#d8e2ff`");`r`n        private static Color primaryFixedDim = ColorTranslator.FromHtml(`"#adc6ff`");")
[System.IO.File]::WriteAllText("top.cs", $text, [System.Text.Encoding]::UTF8)
Write-Host "Fixed top.cs"
