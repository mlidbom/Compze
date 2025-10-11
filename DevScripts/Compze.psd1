@{
    # Script module or binary module file associated with this manifest.
    RootModule = 'Compze.psm1'

    # Version number of this module.
    ModuleVersion = '1.0.0'

    # ID used to uniquely identify this module
    GUID = 'a8d9c7e5-4b3a-4f2e-8d1c-6e9f7a8b2c4d'

    # Author of this module
    Author = 'Compze Development Team'

    # Company or vendor of this module
    CompanyName = 'Compze'

    # Copyright statement for this module
    Copyright = '(c) Compze. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'PowerShell module for Compze development tasks'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Functions to export from this module
    FunctionsToExport = @(
        'Fix-CompzeCsprojExclusions',
        'Remove-CompzeRedundantInternalsVisibleTo',
        'Validate-CompzeSolutionStructure',
        'Clean-Compze',
        'Build-Compze',
        'Test-Compze',
        'Fix-CompzeEncodings',
        'Reload-CompzeModule'
    )

    # Cmdlets to export from this module
    CmdletsToExport = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess
    PrivateData = @{
        PSData = @{
            # Tags applied to this module
            Tags = @('Compze', 'Development', 'Testing')

            # ReleaseNotes of this module
            ReleaseNotes = 'Initial release with development helper functions'
        }
    }
}
