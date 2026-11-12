# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-SolutionRestoreGraphs {
    <#
    .SYNOPSIS
    Restores a solution and reads the restore graph of each of its projects

    .DESCRIPTION
    A project's restore graph is what NuGet's restore resolved for it, as its obj/project.assets.json records it: the
    packages and projects it references directly, every package it ends up with and at which version, what each of
    those requires, and how the project sets its package versions (central package management, transitive pinning).

    A project that targets several frameworks has a graph for each, named "<project> (<framework>)".

    .PARAMETER SolutionPath
    The solution file.

    .OUTPUTS
    An object with:
    - Graphs: for each project, an object with
      - ProjectName
      - UsesCentralPackageManagement: whether its package versions come from a Directory.Packages.props.
      - UsesTransitivePinning: whether those versions also apply to packages it gets only through other packages.
      - CentralPackageVersions: package id to the version Directory.Packages.props names.
      - DirectPackageReferences: package id to the lowest version its reference allows.
      - DirectDependencies: the names of the packages and projects it references directly.
      - Packages: package id to an object with Version (the version resolved) and Dependencies (package or project
        name to the lowest version allowed).
      - Projects: the projects it references, directly or not: project name to an object with Dependencies.
    - ProjectsWithoutRestoreOutput: the solution's projects with no obj/project.assets.json, which no graph describes.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )

    $restoreOutput = dotnet restore $SolutionPath --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Restoring $SolutionPath failed:`n$($restoreOutput -join "`n")" }

    $projectListing = dotnet sln $SolutionPath list 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Listing the projects of $SolutionPath failed:`n$($projectListing -join "`n")" }
    $solutionDirectory = Split-Path -Parent $SolutionPath
    $projectPaths = @($projectListing | Where-Object { $_ -match '\.\w*proj\s*$' } | ForEach-Object { Join-Path $solutionDirectory $_.Trim() })

    $graphs = @()
    $projectsWithoutRestoreOutput = @()
    foreach ($projectPath in $projectPaths) {
        $assetsPath = Join-Path (Split-Path -Parent $projectPath) "obj/project.assets.json"
        if (-not (Test-Path -LiteralPath $assetsPath)) {
            $projectsWithoutRestoreOutput += [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
            continue
        }
        $graphs += ConvertFromRestoreAssets (Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json -AsHashtable) $assetsPath
    }
    return [PSCustomObject]@{ Graphs = $graphs; ProjectsWithoutRestoreOutput = $projectsWithoutRestoreOutput }
}

function ConvertFromRestoreAssets($Assets, [string]$AssetsPath) {
    $restore = $Assets.project.restore
    $frameworks = $Assets.project.frameworks
    foreach ($frameworkName in @($frameworks.Keys)) {
        $framework = $frameworks[$frameworkName]
        $target = $Assets.targets[$frameworkName]
        if ($null -eq $target) { throw "$AssetsPath has no restore graph for its framework $frameworkName." }

        $packages = @{}
        $projects = @{}
        foreach ($libraryKey in @($target.Keys)) {
            $name, $version = $libraryKey -split '/', 2
            $library = $target[$libraryKey]
            $dependencies = ConvertFromDependencyRanges $library['dependencies']
            if ($library.type -eq 'package') { $packages[$name] = [PSCustomObject]@{ Version = $version; Dependencies = $dependencies } }
            elseif ($library.type -eq 'project') { $projects[$name] = [PSCustomObject]@{ Dependencies = $dependencies } }
        }

        $directPackageReferences = @{}
        if ($framework['dependencies']) {
            foreach ($packageId in @($framework.dependencies.Keys | Where-Object { $framework.dependencies[$_].target -eq 'Package' })) {
                $directPackageReferences[$packageId] = GetLowestVersionAllowed $framework.dependencies[$packageId].version
            }
        }
        $centralPackageVersions = @{}
        if ($framework['centralPackageVersions']) {
            foreach ($packageId in @($framework.centralPackageVersions.Keys)) { $centralPackageVersions[$packageId] = $framework.centralPackageVersions[$packageId] }
        }

        [PSCustomObject]@{
            ProjectName                  = if ($frameworks.Count -gt 1) { "$($restore.projectName) ($frameworkName)" } else { $restore.projectName }
            UsesCentralPackageManagement = [bool]$restore['centralPackageVersionsManagementEnabled']
            UsesTransitivePinning        = [bool]$restore['CentralPackageTransitivePinningEnabled']
            CentralPackageVersions       = $centralPackageVersions
            DirectPackageReferences      = $directPackageReferences
            DirectDependencies           = @($Assets.projectFileDependencyGroups[$frameworkName] | ForEach-Object { ($_ -split '\s+')[0] })
            Packages                     = $packages
            Projects                     = $projects
        }
    }
}

function ConvertFromDependencyRanges($DependencyRanges) {
    # Name to the lowest version allowed; empty for a library that depends on nothing.
    $lowestVersions = @{}
    if ($DependencyRanges) {
        foreach ($name in @($DependencyRanges.Keys)) { $lowestVersions[$name] = GetLowestVersionAllowed $DependencyRanges[$name] }
    }
    return $lowestVersions
}

function GetLowestVersionAllowed([string]$VersionRange) {
    # NuGet writes a range as "[0.6.0-alpha, )", and a dependency's floor as "0.6.0-alpha", which means the same.
    if ($VersionRange -notmatch '^[\[\(]?\s*(?<lowest>[^,\s\]\)]+)') { throw "'$VersionRange' names no lowest version." }
    return $Matches.lowest
}
