# A package's changelog (src/<PackageName>/CHANGELOG.md) and its sections, one per version. A version's section is what
# its release says it changed: the text of its GitHub release and its package's release notes. Used by the release
# scripts and by DevScripts. DevScripts dot-sources this file into an interactive PowerShell session, so nothing in it
# may call exit.

function FindProjectChangelogPath([string]$PackageName) {
    # The package's CHANGELOG.md; $null when it has none. Relative to the current directory, which must be the
    # repository root.
    $projectDir = Get-ChildItem -Path "src" -Directory -Filter $PackageName | Select-Object -First 1
    if (-not $projectDir) { return $null }
    $path = Join-Path $projectDir.FullName "CHANGELOG.md"
    if (-not (Test-Path $path)) { return $null }
    return $path
}

function GetChangelogSections([string[]]$ChangelogLines) {
    # The changelog's sections, in the changelog's order, newest first: for each "## <version>" heading, the version and
    # the text up to the next "## " heading, trimmed ($null when there is none). A heading may say more after its
    # version, as "## 0.2.0-alpha.1 *(as Compze.Utilities.Testing.Must)*" does: the version is its first word.
    $sections = @()
    $heading = $null
    $bodyLines = @()
    foreach ($line in $ChangelogLines) {
        if ($line -match '^## ') {
            if ($heading) { $sections += NewChangelogSection $heading $bodyLines }
            $heading = $line
            $bodyLines = @()
        } elseif ($heading) {
            $bodyLines += $line
        }
    }
    if ($heading) { $sections += NewChangelogSection $heading $bodyLines }
    return $sections
}

function ExtractChangelogSectionForVersion([string]$ChangelogPath, [string]$Version) {
    # The text of the version's section in the changelog file; $null when it has no such section, or an empty one.
    $section = @(GetChangelogSections (Get-Content $ChangelogPath)) | Where-Object { $_.Version -eq $Version } | Select-Object -First 1
    if (-not $section) { return $null }
    return $section.Content
}

function NewChangelogSection([string]$Heading, [string[]]$BodyLines) {
    $content = ($BodyLines -join "`n").Trim()
    return [PSCustomObject]@{
        Version = ($Heading.Substring(3).Trim() -split '\s+')[0]
        Content = if ($content) { $content } else { $null }
    }
}
