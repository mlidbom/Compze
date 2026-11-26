# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Restore-NuGetOrgBuilds {
    <#
    .SYNOPSIS
    Restores nuget.org's builds of package versions into a packages folder under Directory, laid out as NuGet lays out the global packages folder; returns each restored version's folder by its Get-PackageVersionKey

    .DESCRIPTION
    One dotnet restore of a solution with a project of its own for each version, referencing that exact version, so that
    the versions restore together and one that fails leaves the others. A NuGet.config of its own makes nuget.org the only
    source, so neither the pending-release feed nor nupkgs/ can serve a version. Empty Directory.Build.props,
    Directory.Build.targets and Directory.Packages.props stop MSBuild from importing any from the folders above.

    Directory should be beside the global packages folder, on the same volume, so that a restored version's folder can
    be renamed into it. A version that doesn't restore from nuget.org is left out of the result, and its failure is
    written as a warning.

    .PARAMETER Packages
    Objects with a PackageId and a Version.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Packages,
        [Parameter(Mandatory = $true)]
        [string]$Directory
    )

    $packagesDirectory = Join-Path $Directory 'packages'
    $projectsDirectory = Join-Path $Directory 'projects'
    New-Item -ItemType Directory -Path $projectsDirectory | Out-Null
    foreach ($stopsTheSearchUpwards in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
        Set-Content -LiteralPath (Join-Path $projectsDirectory $stopsTheSearchUpwards) -Value '<Project />'
    }
    $nuGetConfig = Join-Path $projectsDirectory 'NuGet.config'
    Set-Content -LiteralPath $nuGetConfig -Value @'
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@

    $projectPaths = for ($index = 0; $index -lt $Packages.Count; $index++) {
        $projectPath = "restores-$index/restores-$index.csproj"
        New-Item -ItemType Directory -Path (Join-Path $projectsDirectory "restores-$index") | Out-Null
        Set-Content -LiteralPath (Join-Path $projectsDirectory $projectPath) -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$($Packages[$index].PackageId)" Version="[$($Packages[$index].Version)]" />
  </ItemGroup>
</Project>
"@
        $projectPath
    }
    $solution = Join-Path $projectsDirectory 'RestoresNuGetOrgBuilds.slnx'
    $solutionLines = @('<Solution>') + @($projectPaths | ForEach-Object { '  <Project Path="{0}" />' -f $_ }) + @('</Solution>')
    Set-Content -LiteralPath $solution -Value $solutionLines

    $output = & dotnet restore $solution --packages $packagesDirectory --configfile $nuGetConfig 2>&1
    $restoreFailed = $LASTEXITCODE -ne 0

    $restored = @{}
    foreach ($package in $Packages) {
        if ((Get-NuGetGlobalPackagesEntrySource -GlobalPackagesDirectory $packagesDirectory -PackageId $package.PackageId -Version $package.Version) -like "https://api.nuget.org/*") {
            $restored[(Get-PackageVersionKey -PackageId $package.PackageId -Version $package.Version)] =
                Join-Path $packagesDirectory "$($package.PackageId.ToLowerInvariant())/$($package.Version.ToLowerInvariant())"
        }
    }
    if ($restoreFailed) { Write-Warning "Restoring nuget.org's builds of released Compze versions failed for some:`n$($output -join "`n")" }
    return $restored
}
