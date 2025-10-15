function C-Build-IfNeeded {
    <#
    .SYNOPSIS
    Conditionally builds the solution based on parameters
    
    .DESCRIPTION
    Helper function that handles build logic for test functions.
    Builds the solution unless -NoBuild is specified.
    Optionally performs clean or full git reset before building.
    
    .PARAMETER NoBuild
    Skip building entirely and return true immediately
    
    .PARAMETER Clean
    Performs a deep clean before building
    
    .PARAMETER FullGitReset
    Performs a full git reset that removes all untracked files and directories before building.
    Requires a clean working tree (no uncommitted changes). Implies -Clean.
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    When -WhatIf is used, returns true without building.
    
    .OUTPUTS
    Boolean - $true if build succeeded or was skipped, $false if build failed
    
    .EXAMPLE
    if (-not (C-Build-IfNeeded -Clean)) {
        Write-Error "Build failed!"
        return
    }
    
    .EXAMPLE
    if (-not (C-Build-IfNeeded -NoBuild)) {
        # This will always succeed since NoBuild skips building
    }
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset
    )
    
    # If NoBuild is specified, skip building entirely
    if ($NoBuild) {
        return $true
    }
    
    # If WhatIf is specified with FullGitReset, show what would be deleted and return
    if ($FullGitReset -and $WhatIfPreference) {
        C-Clean -FullGitReset -WhatIf
        return $true
    }
    
    # Perform the build using C-Build
    if ($FullGitReset) {
        C-Build -FullGitReset
    }
    elseif ($Clean) {
        C-Build -Clean
    }
    else {
        C-Build
    }
    
    # Return whether build succeeded
    return $LASTEXITCODE -eq 0
}
