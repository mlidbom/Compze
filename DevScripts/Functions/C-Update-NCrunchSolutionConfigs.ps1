# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Update-NCrunchSolutionConfigs {
    <#
    .SYNOPSIS
    Generates/updates NCrunch .v3.ncrunchsolution files for subset solutions

    .DESCRIPTION
    For each .slnx file in src/, updates the <CustomBuildProperties> element in the
    corresponding .v3.ncrunchsolution file to set UsePackageReference_* = true for
    all switchable src projects NOT in that solution.

    Only the <CustomBuildProperties> element is modified. All other settings,
    engine modes, and formatting in existing files are preserved untouched.

    If no .v3.ncrunchsolution file exists yet, a minimal default is created.

    .EXAMPLE
    C-Update-NCrunchSolutionConfigs
    Updates CustomBuildProperties in all .v3.ncrunchsolution files to match their .slnx files
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    # Parse Directory.Build.props to get all declared UsePackageReference_* properties
    # and their associated .csproj filenames
    $dirBuildPropsPath = Join-Path $script:CompzeSrcRoot "Directory.Build.props"
    $dirBuildPropsContent = Get-Content $dirBuildPropsPath -Raw

    $declaredProperties = [ordered]@{}
    $propertyMatches = [regex]::Matches($dirBuildPropsContent,
        '<(UsePackageReference_\w+)[\s\S]*?Contains\(''[|]([^|]+\.csproj)[|]''\)')
    foreach ($m in $propertyMatches) {
        $propName = $m.Groups[1].Value
        $csprojFile = $m.Groups[2].Value
        $declaredProperties[$propName] = $csprojFile
    }

    # Find all .slnx files in src/
    $slnxFiles = Get-ChildItem -Path $script:CompzeSrcRoot -Filter "*.slnx" -File

    foreach ($slnx in $slnxFiles) {
        $slnxContent = Get-Content $slnx.FullName -Raw

        # Extract project .csproj filenames from the .slnx
        $projectsInSolution = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $slnxProjectMatches = [regex]::Matches($slnxContent, '<Project\s+Path="[^"]*?([^/\\]+\.csproj)"')
        foreach ($m in $slnxProjectMatches) {
            [void]$projectsInSolution.Add($m.Groups[1].Value)
        }

        # Determine which UsePackageReference_* flags must be true
        # (src projects NOT in this solution)
        $customBuildProps = @()
        foreach ($entry in $declaredProperties.GetEnumerator()) {
            $propName = $entry.Key
            $csprojFile = $entry.Value
            if (-not $projectsInSolution.Contains($csprojFile)) {
                $customBuildProps += "$propName = true"
            }
        }

        $ncrunchPath = $slnx.FullName -replace '\.slnx$', '.v3.ncrunchsolution'

        if (Test-Path $ncrunchPath) {
            # Existing file — only update <CustomBuildProperties>, leave everything else untouched
            $xml = [xml](Get-Content $ncrunchPath -Raw)
            $settingsNode = $xml.SolutionConfiguration.Settings

            # Remove existing CustomBuildProperties if present
            $existingCbp = $settingsNode.SelectSingleNode('CustomBuildProperties')
            if ($existingCbp) {
                [void]$settingsNode.RemoveChild($existingCbp)
            }

            # Add new CustomBuildProperties with <Value> children if needed
            if ($customBuildProps.Count -gt 0) {
                $cbpElement = $xml.CreateElement('CustomBuildProperties')
                foreach ($prop in $customBuildProps) {
                    $valueElement = $xml.CreateElement('Value')
                    $valueElement.InnerText = $prop
                    [void]$cbpElement.AppendChild($valueElement)
                }
                [void]$settingsNode.AppendChild($cbpElement)
            }

            Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $ncrunchPath
        } else {
            # New file — only include CustomBuildProperties
            $xml = [xml]'<SolutionConfiguration><Settings /></SolutionConfiguration>'
            $settingsNode = $xml.SelectSingleNode('/SolutionConfiguration/Settings')

            if ($customBuildProps.Count -gt 0) {
                $cbpElement = $xml.CreateElement('CustomBuildProperties')
                foreach ($prop in $customBuildProps) {
                    $valueElement = $xml.CreateElement('Value')
                    $valueElement.InnerText = $prop
                    [void]$cbpElement.AppendChild($valueElement)
                }
                [void]$settingsNode.AppendChild($cbpElement)
            }

            Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $ncrunchPath
        }
    }
}
