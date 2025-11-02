# MSBuild Props Files

This directory contains modular MSBuild property files that are imported by `Directory.Build.props`. Each file has a single, focused responsibility and contains only the *what* (the actual functionality), while the main `Directory.Build.props` file controls the *when* (conditional logic).

## Files

### EnablePackageValidation.props
Enables NuGet package validation.

### EnsurePluggableComponentsConfigExists.props
Copies the `TestUsingPluggableComponentCombinations` configuration file from the example if it doesn't exist. Runs before each build.

### IncludeTestConfigurationFiles.props
Includes the `TestUsingPluggableComponentCombinations` file in the project output.

### IncludeTestAppSettings.props
Includes `test-common-appsettings.json` as `appsettings.json` in the project output.

## Structure Philosophy

- **Modular files** contain only the actual functionality (no conditionals)
- **Main Directory.Build.props** contains all conditional logic determining when to apply each module
- **Single responsibility** - Each file does exactly one thing
- **Self-documenting names** - File names clearly describe their purpose

## Usage in Directory.Build.props

The main file determines which projects are test projects and conditionally imports the test-related props files only for those projects.

