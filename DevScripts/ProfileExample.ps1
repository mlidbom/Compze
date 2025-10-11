# Example PowerShell Profile Addition for Compze Development
# 
# To add this to your PowerShell profile:
# 1. Run: notepad $PROFILE
# 2. Copy the line below to the end of your profile
# 3. Save and restart PowerShell (or run: . $PROFILE)

Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking

# After adding this, you can run these commands from any directory:
# - Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders
# - Remove-RedundantInternalsVisibleTo
# - Validate-SolutionStructure
# - Test-Compze              # Run tests (no build)
# - Test-Compze -Build       # Build then run tests
# - Reload-Profile
