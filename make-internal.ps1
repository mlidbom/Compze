param(
    [string[]]$IncludeProjects
)

$srcDir = Join-Path $PSScriptRoot "src"
$tsvPath = Join-Path $PSScriptRoot "types-can-be-internal.tsv"
$tsv = Get-Content $tsvPath

$changes = 0
$skipped = 0
$errors = [System.Collections.ArrayList]::new()

foreach ($line in $tsv) {
    $parts = $line -split "`t"
    $project = $parts[0]
    $kind = $parts[1]
    $typeName = $parts[2]
    $relPath = $parts[3]

    if ($IncludeProjects.Count -gt 0 -and $project -notin $IncludeProjects) {
        $skipped++
        continue
    }

    $fullPath = Join-Path $srcDir $relPath
    if (-not (Test-Path $fullPath)) {
        [void]$errors.Add("NOT FOUND: $fullPath ($project / $typeName)")
        continue
    }

    $content = Get-Content $fullPath -Raw
    $escapedName = [regex]::Escape($typeName)

    # Match: public [static|sealed|abstract|partial|...] class/interface TypeName
    # The word boundary \b after typeName prevents partial matches
    $pattern = '(?m)^(\s*)public(\s+(?:(?:static|sealed|abstract|partial|readonly|ref|unsafe)\s+)*)(' + $kind + '\s+' + $escapedName + '\b)'

    if ($content -match $pattern) {
        $newContent = $content -replace $pattern, '${1}internal${2}${3}'
        if ($newContent -ne $content) {
            Set-Content $fullPath $newContent -NoNewline
            $changes++
            Write-Host "OK: $project / $typeName"
        } else {
            [void]$errors.Add("NO-OP: $project / $typeName in $relPath")
        }
    } else {
        [void]$errors.Add("NO MATCH: $project / $typeName in $relPath")
    }
}

Write-Host ""
Write-Host "--- Summary ---"
Write-Host "Changes made: $changes"
Write-Host "Skipped (not in project list): $skipped"
Write-Host "Errors: $($errors.Count)"
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    $errors | ForEach-Object { Write-Host "  $_" }
}
