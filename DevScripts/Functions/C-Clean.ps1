# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Clean {
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
    C-Clean
    Performs a deep clean (dotnet clean + delete all \obj\ folders)
    
    .EXAMPLE
    C-Clean -FullGitReset
    Performs a deep clean plus git clean -fdx after backing up TestUsingPluggableComponentCombinations
    
    .EXAMPLE
    C-Clean -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [Parameter(Mandatory = $false)]
        [switch]$FullGitReset
    )
    
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
            
            # Show what would be removed, but filter out TestUsingPluggableComponentCombinations since we protect it
            git clean -fdxn | Where-Object { $_ -notmatch 'src/TestUsingPluggableComponentCombinations' }
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
            $testConfigFile = Join-Path $script:CompzeSrcRoot "TestUsingPluggableComponentCombinations"
            $backupFile = Join-Path $env:TEMP "CompzeTestUsingPluggableComponentCombinations.backup"
            
            if (Test-Path $testConfigFile) {
                Write-Verbose "Backing up TestUsingPluggableComponentCombinations to $backupFile"
                Copy-Item -Path $testConfigFile -Destination $backupFile -Force
            }
        } finally {
            Pop-Location
        }
    }
    
    Push-Location $script:CompzeSrcRoot
    try {
        dotnet clean $script:CompzeSolutionPath | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "dotnet clean reported errors, but continuing..."
        }
        
        $objFolders = Get-ChildItem -Path $script:CompzeSrcRoot -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue
        
        foreach ($folder in $objFolders) {
            try {
                Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction Stop
            } catch {
                Write-Warning "Failed to delete: $($folder.FullName) - $_"
            }
        }

        # Clean nupkgs/ — keep only the latest version of each package
        $nupkgsPath = Join-Path $script:CompzeRoot "nupkgs"
        if (Test-Path $nupkgsPath) {
            Get-ChildItem $nupkgsPath -Filter "*.nupkg" |
                Group-Object { $_.Name -replace '\.\d+\.\d+\.\d+.*\.nupkg$', '' } |
                ForEach-Object {
                    $_.Group | Sort-Object LastWriteTime -Descending | Select-Object -Skip 1 | Remove-Item -Force
                }
            # Also clean orphaned .snupkg files that no longer have a matching .nupkg
            Get-ChildItem $nupkgsPath -Filter "*.snupkg" | ForEach-Object {
                $matchingNupkg = $_.FullName -replace '\.snupkg$', '.nupkg'
                if (-not (Test-Path $matchingNupkg)) {
                    Remove-Item $_.FullName -Force
                }
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
                    $testConfigFile = Join-Path $script:CompzeSrcRoot "TestUsingPluggableComponentCombinations"
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
