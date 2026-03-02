<#
.SYNOPSIS
Uses the ReSharper inspection report character offsets to precisely replace
'public' with 'internal' for MemberCanBeInternal warnings.
The Offset field (e.g., "189-195") gives the character range of the member name.
We search backward from that offset to find the 'public' keyword and replace it.
#>

$srcDir = $PSScriptRoot
$reportPath = Join-Path $srcDir "inspect-report.xml"

[xml]$r = Get-Content $reportPath
$all = $r.Report.Issues.Project | ForEach-Object {
    $p = $_.Name
    $_.Issue | ForEach-Object {
        $_ | Add-Member -NotePropertyName Project -NotePropertyValue $p -PassThru
    }
}

$items = $all | Where-Object TypeId -eq 'MemberCanBeInternal'

$changes = 0
$errors = [System.Collections.ArrayList]::new()

# Group by file to batch changes per file
$byFile = $items | Group-Object File

foreach ($group in $byFile) {
    $relPath = $group.Name
    # Paths in report are relative to src/ dir (e.g., "..\samples\..." or "Compze.Core\...")
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $srcDir "src" $relPath))
    if (-not (Test-Path $fullPath)) {
        [void]$errors.Add("NOT FOUND: $fullPath (from $relPath)")
        continue
    }

    $content = [System.IO.File]::ReadAllText($fullPath)

    # Sort by offset descending so replacements don't shift positions
    $issues = $group.Group | Sort-Object { if($_.Offset -match '^(\d+)') { [int]$Matches[1] } else { 0 } } -Descending

    foreach ($issue in $issues) {
        $name = if ($issue.Message -match "'([^']+)'") { $Matches[1] } else { '?' }

        if ($issue.Offset -notmatch '^(\d+)-(\d+)$') {
            [void]$errors.Add("BAD OFFSET: $name at offset '$($issue.Offset)' in $relPath")
            continue
        }

        $offsetStart = [int]$Matches[1]

        # Search backward from the member name offset for the word 'public'
        # It should be within ~50 chars before the member name (accounting for return types, generics, etc.)
        $searchStart = [Math]::Max(0, $offsetStart - 200)
        $searchRegion = $content.Substring($searchStart, $offsetStart - $searchStart)

        # Find the LAST occurrence of 'public' in this region (closest to the member name)
        $publicPattern = [regex]'\bpublic\b'
        $matches = $publicPattern.Matches($searchRegion)

        if ($matches.Count -eq 0) {
            [void]$errors.Add("NO PUBLIC FOUND: $name at offset $offsetStart in $relPath")
            continue
        }

        $lastMatch = $matches[$matches.Count - 1]
        $absolutePos = $searchStart + $lastMatch.Index

        # Verify the 6 chars at that position are indeed 'public'
        $found = $content.Substring($absolutePos, 6)
        if ($found -ne 'public') {
            [void]$errors.Add("MISMATCH: expected 'public' but found '$found' at pos $absolutePos for $name in $relPath")
            continue
        }

        # Replace 'public' with 'internal' at the exact position
        $content = $content.Substring(0, $absolutePos) + 'internal' + $content.Substring($absolutePos + 6)
        $changes++
        Write-Host "OK: $($issue.Project) | $name | offset $offsetStart | $relPath"
    }

    [System.IO.File]::WriteAllText($fullPath, $content)
}

Write-Host ""
Write-Host "--- Summary ---"
Write-Host "Changes made: $changes"
Write-Host "Errors: $($errors.Count)"
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    $errors | ForEach-Object { Write-Host "  $_" }
}
