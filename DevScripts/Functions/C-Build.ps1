# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Build {
    <#
    .SYNOPSIS
    Builds the Compze solution
    
    .DESCRIPTION
    Builds the Compze solution. Optionally performs a deep clean before building.
    
    .PARAMETER NoBuild
    Skip building entirely. Useful when called from scripts that may or may not need to build.
    When specified, returns immediately with success (exit code 0).
    
    .PARAMETER Clean
    Performs a deep clean before building. This runs 'dotnet clean' and then deletes all \obj\ folders.
    
    .PARAMETER FullGitReset
    Performs a full git reset that removes all untracked files and directories before building.
    This will backup TestUsingPluggableComponentCombinations before running git clean.
    Requires a clean working tree (no uncommitted changes). Implies -Clean.
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    
    .EXAMPLE
    C-Build
    Builds the solution
    
    .EXAMPLE
    C-Build -NoBuild
    Returns immediately without building (useful for scripting)
    
    .EXAMPLE
    C-Build -Clean
    Performs a deep clean (dotnet clean + delete all \obj\ folders) then builds the solution
    
    .EXAMPLE
    C-Build -FullGitReset
    Performs a full git clean, then builds the solution
    
    .EXAMPLE
    C-Build -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset
    )
    
    # If NoBuild is specified, return immediately with success
    if ($NoBuild) {
        $global:LASTEXITCODE = 0
        return
    }
    
    # Ensure pluggable component configuration files exist before building
    C-Set-PluggableComponents -EnsureValid

    # Fail loudly if the .claude/ symlinks into .claude-shared/ have degraded into plain files
    C-Verify-ClaudeConfigSymlinks
    
    Push-Location $script:CompzeSrcRoot
    try {
        if ($FullGitReset) {
            if ($WhatIfPreference) {
                C-Clean -FullGitReset -WhatIf
                return
            } else {
                C-Clean -FullGitReset
            }
        }
        elseif ($Clean) {
            C-Clean
        }
        
        dotnet build $script:CompzeSolutionPath
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed!"
            return
        }
        
        # Set success exit code
        $global:LASTEXITCODE = 0
    } finally {
        Pop-Location
    }
}
