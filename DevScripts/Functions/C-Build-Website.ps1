# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Build-Website {
    <#
    .SYNOPSIS
    Builds the website with DocFX, and fails, listing them, when DocFX reports any warning

    .DESCRIPTION
    For checking a change to the documentation in one step.

    Builds the site in src/Websites/Website as `npm run build-site-only` does, without needing Node.js: it refreshes the
    junctions that bring the co-located _docs folders into DocFX's content (Ensure-CoLocatedDocsJunctions.ps1), then
    runs `docfx build docfx-site-only.json`, which writes the site to src/Websites/Website/_site.
    src/Documentation-CoLocation.README.md describes how the site is put together.

    A DocFX warning is a broken page: a link or a code include that leads nowhere, or a table of contents entry for a
    folder DocFX doesn't have. So a build with any warning fails, and lists the warnings.

    It doesn't compile the pages' example code (the _docs/*.cs files): the Website project does that, in C-Build.

    Sets $LASTEXITCODE to 0 on success and 1 on failure, and reports a failure as an error of its own. On success it
    writes nothing.

    .EXAMPLE
    C-Build-Website
    Builds the website, and fails if DocFX warns
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param()

    $websiteFolder = Join-Path $script:CompzeSrcRoot 'Websites\Website'

    if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) {
        Write-CommandFailure -Command $PSCmdlet -Message "docfx isn't installed. Install it with: dotnet tool install -g docfx"
        return
    }

    & (Join-Path $websiteFolder 'Ensure-CoLocatedDocsJunctions.ps1')

    Push-Location $websiteFolder
    try {
        $docfxOutput = @(docfx build docfx-site-only.json 2>&1 | ForEach-Object { "$_" })
        $docfxExitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }

    if ($docfxExitCode -ne 0) {
        $docfxOutput | Write-Host
        Write-CommandFailure -Command $PSCmdlet -Message "DocFX failed to build the website, with exit code $docfxExitCode."
        return
    }

    $warningCountMatch = $docfxOutput | ForEach-Object { [regex]::Match($_, '^\s*(\d+) warning\(s\)\s*$') } | Where-Object Success | Select-Object -Last 1
    if (-not $warningCountMatch) {
        $docfxOutput | Write-Host
        Write-CommandFailure -Command $PSCmdlet -Message "DocFX's output has no line '<n> warning(s)', so whether it warned can't be told. Its output is above."
        return
    }

    $warningCount = [int]$warningCountMatch.Groups[1].Value
    if ($warningCount -gt 0) {
        $docfxOutput | Where-Object { $_ -match ':\s*warning\b' } | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
        Write-CommandFailure -Command $PSCmdlet -Message "DocFX built the website with $warningCount warning(s), listed above."
        return
    }

    $global:LASTEXITCODE = 0
}
