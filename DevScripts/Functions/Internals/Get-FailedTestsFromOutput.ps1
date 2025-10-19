# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Get-FailedTestsFromOutput {
    <#
    .SYNOPSIS
    Extracts failed test names from dotnet test output
    
    .DESCRIPTION
    Parses the test output to extract the fully qualified test names (TestClass.TestMethod)
    for all failed tests. This is useful for reporting and re-running specific tests.
    
    .PARAMETER TestOutput
    Array of output lines from dotnet test
    
    .OUTPUTS
    Array of PSCustomObject with properties:
    - FullName: Fully qualified test name (Namespace.Class.Method)
    - DisplayName: The test display name shown in the "Failed <name>" line
    
    .EXAMPLE
    $failedTests = Get-FailedTestsFromOutput -TestOutput $testOutput
    foreach ($test in $failedTests) {
        Write-Host "Failed: $($test.FullName)"
    }
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$TestOutput
    )
    
    $failedTests = @()
    
    # Convert to string array if needed
    $lines = @($TestOutput | ForEach-Object { $_.ToString() })
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Look for lines that start with "  Failed <TestName>"
        if ($line -match '^\s+Failed\s+(.+?)\s+\[') {
            $displayName = $matches[1].Trim()
            
            # Now look ahead for the stack trace to find the actual test method
            # We want the LAST method in the stack trace that has a source file location,
            # as that's typically the actual test method (not framework code)
            $fullName = $null
            $lastMethodFound = $null
            for ($j = $i + 1; $j -lt $lines.Count -and $j -lt ($i + 100); $j++) {
                $stackLine = $lines[$j]
                
                # Look for method calls in the stack trace with source file locations
                # Pattern: "   at Namespace.Class.Method() in ..."
                if ($stackLine -match '^\s+at\s+([A-Za-z_][\w.]+)\.([A-Za-z_]\w+)\([^)]*\)\s+in\s+.*\.cs:line\s+\d+') {
                    $className = $matches[1]
                    $methodName = $matches[2]
                    $lastMethodFound = "$className.$methodName"
                    # Don't break - keep looking for the last one
                }
                
                # Stop searching if we hit the next test or a summary line
                if ($stackLine -match '^\s+(Failed|Passed!|Failed!)') {
                    break
                }
            }
            
            # If we found a method in the stack trace, check if the display name contains
            # additional info (like pluggable components) and append it
            if ($lastMethodFound) {
                # Extract the base method name and pluggable component suffix from display name
                # Pattern: method_name(PluggableComponent:Provider) where the parentheses contain the pluggable components
                if ($displayName -match '^(.+?)(\([^)]+:[^)]+\))$') {
                    $baseMethodName = $matches[1]
                    $pluggableComponentSuffix = $matches[2]
                    # Escape the base method name for use in regex matching
                    $escapedBaseMethodName = [regex]::Escape($baseMethodName)
                    # Check if the method found ends with the base name
                    if ($lastMethodFound -match "\.$escapedBaseMethodName$") {
                        # Append the pluggable component suffix to create the full display name
                        $fullName = $lastMethodFound + $pluggableComponentSuffix
                    } else {
                        # Fallback to the display name if pattern doesn't match
                        $fullName = $displayName
                    }
                } else {
                    # No pluggable component suffix, use the method found
                    $fullName = $lastMethodFound
                }
            } else {
                # If we couldn't find it in the stack trace, use the display name
                $fullName = $displayName
            }
            
            $failedTests += [PSCustomObject]@{
                FullName = $fullName
                DisplayName = $displayName
            }
        }
    }
    
    return $failedTests
}
