function Clean-Compze {
    <#
    .SYNOPSIS
    Performs a deep clean of the Compze solution
    
    .DESCRIPTION
    Performs a deep clean by running 'dotnet clean' and then deleting all \obj\ folders.
    
    .EXAMPLE
    Clean-Compze
    Performs a deep clean (dotnet clean + delete all \obj\ folders)
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    $srcPath = Join-Path $script:CompzeRoot "src"
    
    Push-Location $srcPath
    try {
        dotnet clean $solutionPath | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "dotnet clean reported errors, but continuing..."
        }
        
        $objFolders = Get-ChildItem -Path $srcPath -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue
        
        foreach ($folder in $objFolders) {
            try {
                Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction Stop
            } catch {
                Write-Warning "Failed to delete: $($folder.FullName) - $_"
            }
        }
    } finally {
        Pop-Location
    }
}
