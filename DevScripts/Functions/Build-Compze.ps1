function Build-Compze {
    <#
    .SYNOPSIS
    Builds the Compze solution
    
    .DESCRIPTION
    Builds the Compze solution. Optionally performs a deep clean before building.
    
    .PARAMETER Clean
    Performs a deep clean before building. This runs 'dotnet clean' and then deletes all \obj\ folders.
    
    .EXAMPLE
    Build-Compze
    Builds the solution
    
    .EXAMPLE
    Build-Compze -Clean
    Performs a deep clean (dotnet clean + delete all \obj\ folders) then builds the solution
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Clean
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($Clean) {
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
