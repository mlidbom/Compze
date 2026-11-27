# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Invoke-BuildWithValidation {
    <#
    .SYNOPSIS
    Builds the solution and validates the result
    
    .DESCRIPTION
    Helper function that builds the solution with specified parameters and validates the result.
    Handles -WhatIf for FullGitReset and checks for build failures.
    Sets $LASTEXITCODE appropriately and returns boolean success/failure.
    
    .PARAMETER NoBuild
    Skip building the solution
    
    .PARAMETER Clean
    Performs a deep clean before building
    
    .PARAMETER FullGitReset
    Performs a full git reset before building
    
    .OUTPUTS
    Boolean - $true if build succeeded or was skipped, $false if build failed or WhatIf preview shown.
    A caller tells the two $false cases apart by $LASTEXITCODE: 1 after a failed build, 0 after a WhatIf preview.
    
    .EXAMPLE
    if (-not (Invoke-BuildWithValidation -Clean)) {
        return
    }
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset
    )
    
    # What the build writes is captured: left in the pipeline, it became part of what this function returns, and the
    # caller's test of the result read the non-empty array as success, so the tests ran after a failed build.
    $buildOutput = @(C-Build -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset)
    $buildExitCode = $LASTEXITCODE

    # Handle -WhatIf for FullGitReset (returns early after showing what would be deleted)
    if ($FullGitReset -and $WhatIfPreference) {
        $buildOutput | Out-Host
        $global:LASTEXITCODE = 0
        return $false  # Signal to caller to exit early
    }

    # C-Build has reported the failure, and the caller reports its own: this shows what the build said about it.
    if ($buildExitCode -ne 0) {
        $errorLines = @($buildOutput | ForEach-Object { "$_" } | Where-Object { $_ -match ':\s*error\s+[A-Za-z]+\d+' } | Select-Object -Unique)
        if ($errorLines.Count -gt 0) { $errorLines | Out-Host } else { $buildOutput | Select-Object -Last 40 | Out-Host }
        $global:LASTEXITCODE = 1
        return $false
    }

    return $true
}
