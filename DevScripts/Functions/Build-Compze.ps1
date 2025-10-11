function Build-Compze {
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
    
    .EXAMPLE
    Build-Compze
    Builds the solution
    
    .EXAMPLE
    Build-Compze -Clean
    Performs a deep clean (dotnet clean + delete all \obj\ folders) then builds the solution
    
    .EXAMPLE
    Build-Compze -FullGitReset
    Performs a full git clean, then builds the solution
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Clean,
        [switch]$FullGitReset
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($FullGitReset) {
            Clean-Compze -FullGitReset
        }
        elseif ($Clean) {
            Clean-Compze
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
