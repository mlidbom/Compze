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
    Boolean - $true if build succeeded or was skipped, $false if build failed or WhatIf preview shown
    
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
    
    # Build if needed
    C-Build -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset
    
    # Handle -WhatIf for FullGitReset (returns early after showing what would be deleted)
    if ($FullGitReset -and $WhatIfPreference) {
        $global:LASTEXITCODE = 0
        return $false  # Signal to caller to exit early
    }
    
    # Check if build failed
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        $global:LASTEXITCODE = 1
        return $false
    }
    
    return $true
}
