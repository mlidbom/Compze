# Example PowerShell Profile Addition for Compze Development
# 
# To add this to your PowerShell profile:
# 1. Run: notepad $PROFILE
# 2. Copy the line below to the end of your profile
# 3. Save and restart PowerShell (or run: . $PROFILE)

Import-Module C:\Dev\Compze\DevScripts\Compze.psm1 -DisableNameChecking

# After adding this, you can run these commands from any directory:
# Type C-<Tab> to see all available commands, including:
# - C-Test                   # Run tests (no build)
# - C-Test -Build            # Build then run tests
# - C-Build                  # Build solution
# - C-Clean                  # Deep clean
# - C-Get-PluggableComponents    # Show active test configurations
# - C-Set-PluggableComponents    # Configure test combinations
# - C-Reload-Module          # Reload module and profile
# - C-Get-Commands           # List all commands
