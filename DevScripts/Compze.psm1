# Compze Development Module
# This module provides convenient commands for Compze development

$script:CompzeRoot = Split-Path -Parent $PSScriptRoot

# Common paths used across multiple functions
$script:CompzeSrcRoot = Join-Path $script:CompzeRoot "src"
$script:CompzeSolutionPath = Join-Path $script:CompzeSrcRoot "Compze.slnx"

# Import all function files from the Functions directory and collect function names
$functionFiles = Get-ChildItem -Path (Join-Path $PSScriptRoot "Functions") -Filter "*.ps1" -ErrorAction SilentlyContinue
$functionNames = @()

foreach ($file in $functionFiles) {
    . $file.FullName
    
    # Extract function name from file name (assumes file name matches function name)
    # e.g., "Build-Compze.ps1" -> "Build-Compze"
    $functionName = $file.BaseName
    $functionNames += $functionName
}

# Export all loaded functions automatically
Export-ModuleMember -Function $functionNames
