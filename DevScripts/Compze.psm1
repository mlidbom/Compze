# Compze Development Module
# This module provides convenient commands for Compze development

$script:CompzeRoot = Split-Path -Parent $PSScriptRoot

# Import all function files from the Functions directory
$functionFiles = Get-ChildItem -Path (Join-Path $PSScriptRoot "Functions") -Filter "*.ps1" -ErrorAction SilentlyContinue

foreach ($file in $functionFiles) {
    . $file.FullName
}

# Export the functions
Export-ModuleMember -Function @(
    'Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders',
    'Remove-CompzeRedundantInternalsVisibleTo', 
    'Validate-CompzeSolutionStructure',
    'Clean-Compze',
    'Build-Compze',
    'Test-Compze',
    'Fix-CompzeEncodings',
    'Get-CompzeCommands',
    'Reload-CompzeModule'
)
