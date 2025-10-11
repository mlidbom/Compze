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
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    
    .EXAMPLE
    Clean-Compze
    Performs a deep clean (dotnet clean + delete all \obj\ folders)
    
    .EXAMPLE
    Clean-Compze -FullGitReset
    Performs a deep clean plus git clean -fdx after backing up TestUsingPluggableComponentCombinations
    
    .EXAMPLE
    Clean-Compze -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $false)]
        [switch]$FullGitReset
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    $srcPath = Join-Path $script:CompzeRoot "src"
    
    # If FullGitReset with WhatIf, skip normal clean and just show git clean preview
    if ($FullGitReset -and $WhatIfPreference) {
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
            
            git clean -fdxn
        } finally {
            Pop-Location
        }
        return
    }
    
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
            
            # Backup TestUsingPluggableComponentCombinations to temp directory (outside git repo)
            $testConfigFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations"
            $backupFile = Join-Path $env:TEMP "CompzeTestUsingPluggableComponentCombinations.backup"
            
            if (Test-Path $testConfigFile) {
                Write-Verbose "Backing up TestUsingPluggableComponentCombinations to $backupFile"
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
            Write-Verbose "Running git clean -fdx to remove all untracked files and directories..."
            Push-Location $script:CompzeRoot
            try {
                if ($VerbosePreference -eq 'Continue') {
                    git clean -fdx
                } else {
                    # Redirect stdout to null but let stderr through for errors
                    git clean -fdx 2>&1 | ForEach-Object {
                        if ($_ -is [System.Management.Automation.ErrorRecord]) {
                            Write-Error $_
                        }
                    }
                }
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "git clean -fdx failed with exit code $LASTEXITCODE"
                } else {
                    Write-Verbose "Git clean completed successfully."
                    
                    # Restore the backup from temp directory
                    $testConfigFile = Join-Path $srcPath "TestUsingPluggableComponentCombinations"
                    $backupFile = Join-Path $env:TEMP "CompzeTestUsingPluggableComponentCombinations.backup"
                    
                    if (Test-Path $backupFile) {
                        Write-Verbose "Restoring TestUsingPluggableComponentCombinations from backup"
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
