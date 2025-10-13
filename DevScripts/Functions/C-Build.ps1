function C-Build {
    <#
    .SYNOPSIS
    Builds the Compze solution
    
    .DESCRIPTION
    Builds the Compze solution. Optionally performs a deep clean before building.
    
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
    param(
        [switch]$Clean,
        [switch]$FullGitReset
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
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
        
        dotnet build $solutionPath
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed!"
            return
        }
    } finally {
        Pop-Location
    }
}
