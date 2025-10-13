function C-Remove-RedundantInternalsVisibleTo {
    <#
    .SYNOPSIS
    Removes redundant InternalsVisibleTo attributes
    
    .DESCRIPTION
    Tests which InternalsVisibleTo attributes are actually needed by temporarily
    commenting them out and testing the build. Removes attributes that are not needed.
    This is a long-running operation that builds the solution multiple times.
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)
    
    .PARAMETER LogFile
    Path to the log file (defaults to InternalsVisibleTo-Test-Results.log in the workspace root)
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$SolutionPath,
        [string]$LogFile
    )
    
    # Set default values if not provided
    if (-not $SolutionPath) {
        $SolutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    }
    if (-not $LogFile) {
        $LogFile = Join-Path $script:CompzeRoot "InternalsVisibleTo-Test-Results.log"
    }

    # Initialize logging
    $logPath = $LogFile
    $startTime = Get-Date
    Write-Host "Starting InternalsVisibleTo cleanup test at $startTime" -ForegroundColor Green
    "Starting InternalsVisibleTo cleanup test at $startTime" | Out-File -FilePath $logPath -Encoding UTF8

    # Results tracking
    $results = @{
        Removed = @()
        Kept = @()
        Errors = @()
    }

    # Find all InternalsVisibleTo attributes in .csproj files
    Write-Host "Finding all InternalsVisibleTo attributes..." -ForegroundColor Yellow
    $internalsVisibleToMatches = @()

    # Parse the .slnx file to get all project paths
    Write-Host "Reading projects from solution file: $SolutionPath" -ForegroundColor Yellow
    [xml]$slnxContent = Get-Content $SolutionPath
    $solutionDir = Split-Path -Parent $SolutionPath

    # Get all project paths from the solution
    $projectPaths = $slnxContent.Solution.Folder.Project.Path
    if ($projectPaths) {
        $csprojFiles = $projectPaths | ForEach-Object {
            $projectPath = Join-Path $solutionDir $_
            if (Test-Path $projectPath) {
                Get-Item $projectPath
            } else {
                Write-Warning "Project file not found: $projectPath"
            }
        }
    } else {
        Write-Error "No projects found in solution file"
        exit 1
    }

    Write-Host "Found $($csprojFiles.Count) projects in solution" -ForegroundColor Cyan

    foreach ($file in $csprojFiles) {
        $lines = Get-Content $file.FullName
        
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match 'InternalsVisibleTo\s+Include="([^"]+)"') {
                $internalsVisibleToMatches += @{
                    File = $file.FullName
                    LineNumber = $i + 1
                    Line = $lines[$i].Trim()
                    AssemblyName = $matches[1]
                    OriginalLine = $lines[$i]
                }
            }
        }
    }

    Write-Host "Found $($internalsVisibleToMatches.Count) InternalsVisibleTo attributes to test" -ForegroundColor Yellow
    "Found $($internalsVisibleToMatches.Count) InternalsVisibleTo attributes to test" | Out-File -FilePath $logPath -Append -Encoding UTF8

    # Function to perform initial build test
    function Test-InitialBuild {
        Write-Host "Performing initial build test..." -ForegroundColor Yellow
        "Performing initial build test..." | Out-File -FilePath $logPath -Append -Encoding UTF8
        
        $buildResult = & dotnet build $SolutionPath --verbosity quiet 2>&1
        $buildExitCode = $LASTEXITCODE
        
        if ($buildExitCode -ne 0) {
            Write-Host "Initial build failed! Cannot proceed with testing." -ForegroundColor Red
            "Initial build failed! Cannot proceed with testing." | Out-File -FilePath $logPath -Append -Encoding UTF8
            "Build output: $buildResult" | Out-File -FilePath $logPath -Append -Encoding UTF8
            return $false
        }
        
        Write-Host "Initial build successful" -ForegroundColor Green
        "Initial build successful" | Out-File -FilePath $logPath -Append -Encoding UTF8
        return $true
    }

    # Function to comment out an InternalsVisibleTo line
    function Set-InternalsVisibleToCommented {
        param($filePath, $lineNumber, $comment = $true)
        
        $lines = Get-Content $filePath
        if ($comment) {
            # Comment out the line
            $lines[$lineNumber - 1] = "      <!-- " + $lines[$lineNumber - 1].Trim() + " -->"
        } else {
            # Restore the line (remove comment)
            $originalLine = $lines[$lineNumber - 1] -replace "^\s*<!--\s*", "" -replace "\s*-->\s*$", ""
            $lines[$lineNumber - 1] = "      " + $originalLine
        }
        
        $lines | Set-Content $filePath -Encoding UTF8
    }

    # Function to remove commented out InternalsVisibleTo lines
    function Remove-CommentedInternalsVisibleTo {
        param($filePath)
        
        $content = Get-Content $filePath
        $modifiedContent = @()
        
        foreach ($line in $content) {
            # Skip lines that are commented out InternalsVisibleTo attributes
            if ($line -match '^\s*<!--\s*<InternalsVisibleTo\s+Include="[^"]+"\s*/>\s*-->\s*$') {
                Write-Host "    Removing: $($line.Trim())" -ForegroundColor DarkGray
            } else {
                $modifiedContent += $line
            }
        }
        
        $modifiedContent | Set-Content $filePath -Encoding UTF8
    }

    # Function to test build after commenting out an attribute
    function Test-BuildAfterComment {
        param($attributeInfo)
        
        $file = $attributeInfo.File
        $lineNumber = $attributeInfo.LineNumber
        $assemblyName = $attributeInfo.AssemblyName
        
        Write-Host "Testing: $assemblyName in $(Split-Path $file -Leaf)" -ForegroundColor Cyan
        $testMessage = "Testing: $assemblyName in $(Split-Path $file -Leaf) at line $lineNumber"
        $testMessage | Out-File -FilePath $logPath -Append -Encoding UTF8
        
        try {
            # Comment out the attribute
            Set-InternalsVisibleToCommented -filePath $file -lineNumber $lineNumber -comment $true
            
            # Test build
            $buildResult = & dotnet build $SolutionPath --verbosity quiet 2>&1
            $buildExitCode = $LASTEXITCODE
            
            if ($buildExitCode -eq 0) {
                # Build succeeded - we can remove this attribute
                Write-Host "  ✓ REMOVED: Build succeeded without this attribute" -ForegroundColor Green
                "  ✓ REMOVED: Build succeeded without this attribute" | Out-File -FilePath $logPath -Append -Encoding UTF8
                
                $results.Removed += @{
                    File = $file
                    LineNumber = $lineNumber
                    AssemblyName = $assemblyName
                    Line = $attributeInfo.Line
                }
                
                return $true
            } else {
                # Build failed - we need this attribute
                Write-Host "  ✗ KEPT: Build failed without this attribute" -ForegroundColor Red
                "  ✗ KEPT: Build failed without this attribute" | Out-File -FilePath $logPath -Append -Encoding UTF8
                
                # Restore the attribute
                Set-InternalsVisibleToCommented -filePath $file -lineNumber $lineNumber -comment $false
                
                $results.Kept += @{
                    File = $file
                    LineNumber = $lineNumber
                    AssemblyName = $assemblyName
                    Line = $attributeInfo.Line
                    BuildError = $buildResult
                }
                
                return $false
            }
        }
        catch {
            Write-Host "  ⚠ ERROR: Exception occurred during testing" -ForegroundColor Magenta
            "  ⚠ ERROR: Exception occurred: $($_.Exception.Message)" | Out-File -FilePath $logPath -Append -Encoding UTF8
            
            # Try to restore the attribute in case of error
            try {
                Set-InternalsVisibleToCommented -filePath $file -lineNumber $lineNumber -comment $false
            } catch {
                "  Failed to restore attribute after error: $($_.Exception.Message)" | Out-File -FilePath $logPath -Append -Encoding UTF8
            }
            
            $results.Errors += @{
                File = $file
                LineNumber = $lineNumber
                AssemblyName = $assemblyName
                Error = $_.Exception.Message
            }
            
            return $false
        }
    }

    # Main execution
    if (-not (Test-InitialBuild)) {
        exit 1
    }

    Write-Host "`nStarting systematic testing of InternalsVisibleTo attributes..." -ForegroundColor Yellow
    "Starting systematic testing of InternalsVisibleTo attributes..." | Out-File -FilePath $logPath -Append -Encoding UTF8

    $processedCount = 0
    $totalCount = $internalsVisibleToMatches.Count

    foreach ($attribute in $internalsVisibleToMatches) {
        $processedCount++
        Write-Host "`nProgress: $processedCount/$totalCount" -ForegroundColor Yellow
        
        Test-BuildAfterComment -attributeInfo $attribute
        
        # Show progress every 10 items
        if ($processedCount % 10 -eq 0) {
            Write-Host "Processed $processedCount/$totalCount attributes..." -ForegroundColor Yellow
        }
    }

    # Generate summary report
    $endTime = Get-Date
    $duration = $endTime - $startTime

    Write-Host "`n" + "="*60 -ForegroundColor Yellow
    Write-Host "SUMMARY REPORT" -ForegroundColor Yellow
    Write-Host "="*60 -ForegroundColor Yellow

    $summaryReport = @"

================================================================================
INTERNALSVISIBLETO CLEANUP SUMMARY REPORT
================================================================================
Test completed at: $endTime
Duration: $($duration.TotalMinutes.ToString("F2")) minutes
Total attributes tested: $totalCount

RESULTS:
- Removed (unnecessary): $($results.Removed.Count)
- Kept (required): $($results.Kept.Count)  
- Errors: $($results.Errors.Count)

REMOVED ATTRIBUTES (Build succeeded without them):
"@

    $summaryReport | Out-File -FilePath $logPath -Append -Encoding UTF8
    Write-Host $summaryReport

    foreach ($removed in $results.Removed) {
        $removedInfo = "  - $(Split-Path $removed.File -Leaf):$($removed.LineNumber) - $($removed.AssemblyName)"
        Write-Host $removedInfo -ForegroundColor Green
        $removedInfo | Out-File -FilePath $logPath -Append -Encoding UTF8
    }

    Write-Host "`nKEPT ATTRIBUTES (Build failed without them):" -ForegroundColor Yellow
    "KEPT ATTRIBUTES (Build failed without them):" | Out-File -FilePath $logPath -Append -Encoding UTF8

    foreach ($kept in $results.Kept) {
        $keptInfo = "  - $(Split-Path $kept.File -Leaf):$($kept.LineNumber) - $($kept.AssemblyName)"
        Write-Host $keptInfo -ForegroundColor Red
        $keptInfo | Out-File -FilePath $logPath -Append -Encoding UTF8
    }

    if ($results.Errors.Count -gt 0) {
        Write-Host "`nERRORS ENCOUNTERED:" -ForegroundColor Magenta
        "ERRORS ENCOUNTERED:" | Out-File -FilePath $logPath -Append -Encoding UTF8
        
        foreach ($errorItem in $results.Errors) {
            $errorInfo = "  - $(Split-Path $errorItem.File -Leaf):$($errorItem.LineNumber) - $($errorItem.AssemblyName) - Error: $($errorItem.Error)"
            Write-Host $errorInfo -ForegroundColor Magenta
            $errorInfo | Out-File -FilePath $logPath -Append -Encoding UTF8
        }
    }

    Write-Host "`nFinal verification build..." -ForegroundColor Yellow
    "Final verification build..." | Out-File -FilePath $logPath -Append -Encoding UTF8

    $finalBuildResult = & dotnet build $SolutionPath --verbosity quiet 2>&1
    $finalBuildExitCode = $LASTEXITCODE

    if ($finalBuildExitCode -eq 0) {
        Write-Host "✓ Final verification build PASSED" -ForegroundColor Green
        "✓ Final verification build PASSED" | Out-File -FilePath $logPath -Append -Encoding UTF8
    } else {
        Write-Host "✗ Final verification build FAILED" -ForegroundColor Red
        "✗ Final verification build FAILED" | Out-File -FilePath $logPath -Append -Encoding UTF8
        "Final build error: $finalBuildResult" | Out-File -FilePath $logPath -Append -Encoding UTF8
    }

    Write-Host "`nCleaning up: Removing commented out InternalsVisibleTo attributes..." -ForegroundColor Yellow
    "Cleaning up: Removing commented out InternalsVisibleTo attributes..." | Out-File -FilePath $logPath -Append -Encoding UTF8

    # Remove all commented out InternalsVisibleTo attributes from modified files
    $modifiedFiles = @()
    foreach ($removed in $results.Removed) {
        if ($modifiedFiles -notcontains $removed.File) {
            $modifiedFiles += $removed.File
        }
    }

    foreach ($file in $modifiedFiles) {
        Write-Host "  Cleaning: $(Split-Path $file -Leaf)" -ForegroundColor Cyan
        "  Cleaning: $(Split-Path $file -Leaf)" | Out-File -FilePath $logPath -Append -Encoding UTF8
        Remove-CommentedInternalsVisibleTo -filePath $file
    }

    Write-Host "`nFinal cleanup build..." -ForegroundColor Yellow
    "Final cleanup build..." | Out-File -FilePath $logPath -Append -Encoding UTF8

    $cleanupBuildResult = & dotnet build $SolutionPath --verbosity quiet 2>&1
    $cleanupBuildExitCode = $LASTEXITCODE

    if ($cleanupBuildExitCode -eq 0) {
        Write-Host "✓ Final cleanup build PASSED" -ForegroundColor Green
        "✓ Final cleanup build PASSED" | Out-File -FilePath $logPath -Append -Encoding UTF8
    } else {
        Write-Host "✗ Final cleanup build FAILED" -ForegroundColor Red
        "✗ Final cleanup build FAILED" | Out-File -FilePath $logPath -Append -Encoding UTF8
        "Final cleanup build error: $cleanupBuildResult" | Out-File -FilePath $logPath -Append -Encoding UTF8
    }

    Write-Host "`nTest completed! Results saved to: $logPath" -ForegroundColor Green
    Write-Host "Removed $($results.Removed.Count) unnecessary InternalsVisibleTo attributes" -ForegroundColor Green
    Write-Host "Kept $($results.Kept.Count) required InternalsVisibleTo attributes" -ForegroundColor Yellow
    Write-Host "Cleaned up $($modifiedFiles.Count) project files" -ForegroundColor Green

    # Return results object for further processing if needed
    return $results
}
