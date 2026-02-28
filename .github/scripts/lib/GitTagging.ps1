function FormatTag([string]$PackageName, [string]$Version) {
    return "$PackageName/v$Version"
}

function TestTagExists([string]$Tag) {
    git rev-parse "refs/tags/$Tag" 2>$null | Out-Null
    return $LASTEXITCODE -eq 0
}

function PackagesWithNoMatchingReleaseTag($Packages) {
    return @($Packages | Where-Object { -not (TestTagExists $_.Tag) })
}
