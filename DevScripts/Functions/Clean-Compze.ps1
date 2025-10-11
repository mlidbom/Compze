function Clean-Compze {
    <#
    .SYNOPSIS
    Performs a deep clean of the Compze solution
    
    .DESCRIPTION
    Performs a deep clean by running 'dotnet clean' and then deleting all \obj\ folders.
    With -FullGitReset, performs a full git clean after backing up TestUsingPluggableComponentCombinations.
    
    .PARAMETER FullGitReset
    When specified, performs a full git reset that removes all untracked files and directories.
    This will backup TestUsingPluggableComponentCombinations before running git clean.
    Requires a clean working tree (no uncommitted changes).
    
    .EXAMPLE
    Clean-Compze
    Performs a deep clean (dotnet clean + delete all \obj\ folders)
    
    .EXAMPLE
    Clean-Compze -FullGitReset
    Performs a deep clean plus git clean -fdx after backing up TestUsingPluggableComponentCombinations
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $false)]
        [switch]$FullGitReset
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    $srcPath = Join-Path $script:CompzeRoot "src"
    
    # If FullGitReset is specified, check for uncommitted changes first
    if ($FullGitReset) {
        Push-Location $script:CompzeRoot
        try {
            # Check git status
            $gitStatus = git status --porcelain
            if ($gitStatus) {
                Write-Error "Cannot perform FullGitReset: There are uncommitted changes in the repository."
                Write-Host "Git status shows:"
                Write-Host $gitStatus
                return
            }
            
            # Backup TestUsingPluggableComponentCombinations
            $testConfigFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations"
            $backupFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations.backup"
            
            if (Test-Path $testConfigFile) {
                Write-Host "Backing up TestUsingPluggableComponentCombinations to TestUsingPluggableComponentCombinations.backup"
                Copy-Item -Path $testConfigFile -Destination $backupFile -Force
            }
        } finally {
            Pop-Location
        }
    }
    
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
        
        # If FullGitReset is specified, run git clean
        if ($FullGitReset) {
            Write-Host "Running git clean -fdx to remove all untracked files and directories..."
            Push-Location $script:CompzeRoot
            try {
                git clean -fdx
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "git clean -fdx failed with exit code $LASTEXITCODE"
                } else {
                    Write-Host "Git clean completed successfully."
                    
                    # Restore the backup
                    $testConfigFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations"
                    $backupFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations.backup"
                    
                    if (Test-Path $backupFile) {
                        Write-Host "Restoring TestUsingPluggableComponentCombinations from backup"
                        Copy-Item -Path $backupFile -Destination $testConfigFile -Force
                        Remove-Item -Path $backupFile -Force
                    }
                }
            } finally {
                Pop-Location
            }
        }
    } finally {
        Pop-Location
    }
}
