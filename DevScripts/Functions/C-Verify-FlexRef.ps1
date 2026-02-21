# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Verify-FlexRef {
    <#
    .SYNOPSIS
    Validates the FlexRef conditional reference setup

    .DESCRIPTION
    Checks consistency of the FlexRef infrastructure:
    1. Every switchable ProjectReference in a .csproj has the matching conditional PackageReference pair
    2. Every conditional PackageReference pair has a matching UsePackageReference_* property in Directory.Build.props
    3. Package versions in conditional PackageReference blocks are consistent
    4. No orphaned UsePackageReference_* properties (project was renamed/removed)
    5. No unconverted switchable ProjectReferences (plain ProjectReference to a switchable src project)

    .EXAMPLE
    C-Verify-FlexRef
    Validates the FlexRef setup and reports any issues
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $violations = @()

    # Parse Directory.Build.props to extract declared UsePackageReference_* properties
    $dirBuildPropsPath = Join-Path $script:CompzeSrcRoot "Directory.Build.props"
    $dirBuildPropsContent = Get-Content $dirBuildPropsPath -Raw

    # Extract all declared UsePackageReference_* property names and their associated .csproj filenames
    $declaredProperties = @{}
    $propertyMatches = [regex]::Matches($dirBuildPropsContent,
        '<(UsePackageReference_\w+)[\s\S]*?Contains\(''[|]([^|]+\.csproj)[|]''\)')
    foreach ($m in $propertyMatches) {
        $propName = $m.Groups[1].Value
        $csprojFile = $m.Groups[2].Value
        $packageName = $csprojFile -replace '\.csproj$', ''
        $declaredProperties[$propName] = $packageName
    }

    # Build set of switchable package names (the ones with UsePackageReference_* declarations)
    $switchablePackages = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($pkg in $declaredProperties.Values) {
        [void]$switchablePackages.Add($pkg)
    }

    # Verify each declared property corresponds to an actual src project
    foreach ($entry in $declaredProperties.GetEnumerator()) {
        $propName = $entry.Key
        $packageName = $entry.Value
        $projectDir = Join-Path $script:CompzeSrcRoot $packageName
        $projectFile = Join-Path $projectDir "$packageName.csproj"
        if (-not (Test-Path $projectFile)) {
            $violations += "ORPHANED PROPERTY: $propName in Directory.Build.props — project '$packageName' not found at $projectFile"
        }
    }

    # Find all .csproj files (exclude InternalizedSourceReferences sub-solution, obj, bin)
    $csprojFiles = Get-ChildItem -Path $script:CompzeRoot -Filter "*.csproj" -Recurse |
        Where-Object { $_.FullName -notmatch 'Compze\.Build\.InternalizedSourceReferences' } |
        Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

    $expectedVersion = $null

    foreach ($csproj in $csprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        $relativePath = $csproj.FullName.Substring($script:CompzeRoot.Length + 1).Replace("\", "/")

        # Extract all conditional PackageReference pairs (UsePackageReference_*)
        $pkgRefMatches = [regex]::Matches($content,
            "Condition=""'\`\$\((UsePackageReference_\w+)\)' == 'true'"">\s*<PackageReference Include=""([^""]+)"" Version=""([^""]+)""")

        foreach ($m in $pkgRefMatches) {
            $propName = $m.Groups[1].Value
            $pkgName = $m.Groups[2].Value
            $version = $m.Groups[3].Value

            # Check property is declared in Directory.Build.props
            if (-not $declaredProperties.ContainsKey($propName)) {
                $violations += "UNDECLARED PROPERTY: $relativePath uses '$propName' but it's not declared in Directory.Build.props"
            }

            # Check version consistency
            if ($null -eq $expectedVersion) {
                $expectedVersion = $version
            } elseif ($version -ne $expectedVersion) {
                $violations += "VERSION MISMATCH: $relativePath has '$pkgName' at version '$version' (expected '$expectedVersion')"
            }

            # Check the matching ProjectReference pair exists
            $projRefCondition = "'`$($propName)' != 'true'"
            if ($content -notmatch [regex]::Escape("Condition=""'`$($propName)' != 'true'""")) {
                $violations += "MISSING PROJECT-REF PAIR: $relativePath has PackageReference for '$pkgName' but no matching ProjectReference conditional"
            }
        }

        # Check for unconverted plain ProjectReferences to switchable packages
        # Match ProjectReferences NOT inside a conditional ItemGroup
        $projRefMatches = [regex]::Matches($content,
            '<ProjectReference\s+Include="([^"]+)"')
        foreach ($m in $projRefMatches) {
            $include = $m.Groups[1].Value
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($include)

            if ($switchablePackages.Contains($fileName)) {
                # Check if this ProjectReference is inside a conditional ItemGroup
                $lineIndex = $content.Substring(0, $m.Index).LastIndexOf("`n")
                $preceding = $content.Substring(0, $m.Index)

                # Find the nearest preceding <ItemGroup — check if it has a UsePackageReference condition
                $igMatch = [regex]::Match($preceding, '<ItemGroup[^>]*Condition="[^"]*UsePackageReference_[^"]*"[^>]*>\s*$',
                    [System.Text.RegularExpressions.RegexOptions]::RightToLeft)

                if (-not $igMatch.Success) {
                    $violations += "UNCONVERTED REF: $relativePath has plain ProjectReference to switchable package '$fileName'"
                }
            }
        }
    }

    if ($violations.Count -eq 0) {
        Write-Host "FlexRef verification passed — all references are consistent"
    } else {
        Write-Host "FlexRef verification FAILED — $($violations.Count) issue(s):" -ForegroundColor Red
        foreach ($v in $violations) {
            Write-Host "  - $v" -ForegroundColor Yellow
        }
    }

    return $violations
}
