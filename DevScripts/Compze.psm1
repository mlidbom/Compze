# Compze Development Module
# This module provides convenient commands for Compze development

$script:CompzeRoot = Split-Path -Parent $PSScriptRoot

# Common paths used across multiple functions
$script:CompzeSrcRoot = Join-Path $script:CompzeRoot "src"
$script:CompzeSolutionPath = Join-Path $script:CompzeSrcRoot "Compze.AllProjects.slnx"

# Import all function files from the Functions directory and collect function names
$functionFiles = @()
$functionFiles += Get-ChildItem -Path (Join-Path $PSScriptRoot "Functions") -Filter "*.ps1" -ErrorAction SilentlyContinue
$functionFiles += Get-ChildItem -Path (Join-Path $PSScriptRoot "Functions\Internals") -Filter "*.ps1" -ErrorAction SilentlyContinue

$functionNames = @()

foreach ($file in $functionFiles) {
    . $file.FullName
    
    # Extract function name from file name (assumes file name matches function name)
    $functionName = $file.BaseName
    $functionNames += $functionName
}

# Export all functions. The C- prefix indicates "public" API functions.
Export-ModuleMember -Function $functionNames
