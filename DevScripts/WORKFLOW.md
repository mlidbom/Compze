# Development Workflow with Compze Module

## Making Changes to DevScripts

When you edit any files in the `DevScripts` folder (like `Compze.psm1`), you need to reload the module to see your changes.

### Quick Reload

Simply run:
```powershell
Reload-Profile
```

This will:
1. Force-reload the Compze module from disk (picking up any changes)
2. Reload your PowerShell profile
3. Show confirmation that everything reloaded successfully

### What Gets Reloaded

- ✅ All function changes in `Compze.psm1`
- ✅ Any profile customizations in your `$PROFILE`
- ✅ Environment variables and settings from your profile

### No Restart Needed

You **never** need to restart PowerShell when:
- Editing DevScripts functions
- Adding new parameters
- Changing documentation
- Modifying profile settings

Just run `Reload-Profile` and continue working!

## Example Workflow

1. Edit `Compze.psm1` to add a feature
2. Save the file
3. Run `Reload-Profile` in your terminal
4. Test your changes immediately
5. Repeat as needed

## Troubleshooting

If `Reload-Profile` doesn't seem to pick up changes:
- Make sure you saved the file you edited
- Check that you're editing the right file (`C:\Dev\Compze\DevScripts\Compze.psm1`)
- Try running manually: `Import-Module C:\Dev\Compze\DevScripts\Compze.psm1 -DisableNameChecking -Force -Global`
