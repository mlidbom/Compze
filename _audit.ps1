$csprojs = Get-ChildItem -Path "C:\Dev\Compze.worktrees\compze_worktree_2\src" -Filter "*.csproj" -Recurse
$packable = @()
foreach ($f in $csprojs) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    if ($content -match '<IsPackable>false') { continue }
    if ($content -match '<IsTestProject>true') { continue }
    if ($content -match '<Version>') {
        $ver = ""
        if ($content -match '<Version>([^<]+)') { $ver = $Matches[1] }
        $dir = $f.DirectoryName
        $name = $f.Directory.Name
        $hasReadme = [System.IO.File]::Exists([System.IO.Path]::Combine($dir, "README.md"))
        $hasChangelog = [System.IO.File]::Exists([System.IO.Path]::Combine($dir, "CHANGELOG.md"))
        $packable += [PSCustomObject]@{Name=$name; Version=$ver; HasReadme=$hasReadme; HasChangelog=$hasChangelog; Dir=$dir}
    }
}
Write-Host "=== ALL PACKABLE PROJECTS ==="
$packable | Sort-Object Name | ForEach-Object { Write-Host "$($_.Name) | Ver=$($_.Version) | README=$($_.HasReadme) | CHANGELOG=$($_.HasChangelog)" }
Write-Host ""
Write-Host "=== MISSING README ==="
$packable | Where-Object { -not $_.HasReadme } | Sort-Object Name | ForEach-Object { Write-Host "$($_.Name) | Ver=$($_.Version)" }
Write-Host ""
Write-Host "=== MISSING CHANGELOG ==="
$packable | Where-Object { -not $_.HasChangelog } | Sort-Object Name | ForEach-Object { Write-Host "$($_.Name) | Ver=$($_.Version)" }
