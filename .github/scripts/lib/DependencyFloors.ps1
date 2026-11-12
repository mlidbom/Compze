# The dependency floors a package version raises (see ConvertFromPackageManifestXml in PackageManifests.ps1 for what a
# floor is), and the line a changelog section must have that states the ones raised across a minor version.
#
# Why that line must be there: a version's floors are the versions its Compze dependencies had when it was packed, so the
# first release of a package after a minor release of one of its dependencies raises that floor, whatever the package's
# own change. Compze.Tessaging 0.6.1-alpha changed one log level, a patch, and required Compze.Threading 0.10.0-alpha
# instead of 0.9.0-alpha, and Compze.Internals.Logging 0.5.0-alpha instead of 0.4.1-alpha: minor releases, with changes a
# consumer meets. A consumer can't upgrade to the version without them, and its changelog section is where it has to
# learn that.
#
# Used by the release scripts and by DevScripts. DevScripts dot-sources this file into an interactive PowerShell session,
# so nothing in it may call exit.

$script:RaisedDependencyFloorsLineLead = '- **Requires newer minor versions of other Compze packages:**'

function TestCrossesAMinorVersion([string]$FromVersion, [string]$ToVersion) {
    # Whether the two versions differ in their major or minor version: whether, by Compze's versioning, the changes
    # between them may include ones a consumer meets.
    $from = [semver]$FromVersion
    $to = [semver]$ToVersion
    return $from.Major -ne $to.Major -or $from.Minor -ne $to.Minor
}

function GetDependencyFloorsRaisedAcrossAMinorVersion($PreviousManifest, $Manifest) {
    # The Compze dependency floors of the manifest that are a minor version or more above the previous version's, as
    # objects with PackageId, PreviousFloor and Floor, ordered by package id. A dependency the previous version didn't
    # have raises no floor.
    foreach ($packageId in $Manifest.CompzeDependencyFloors.Keys) {
        if (-not $PreviousManifest.CompzeDependencyFloors.Contains($packageId)) { continue }
        $previousFloor = $PreviousManifest.CompzeDependencyFloors[$packageId]
        $floor = $Manifest.CompzeDependencyFloors[$packageId]
        if (([semver]$floor -gt [semver]$previousFloor) -and (TestCrossesAMinorVersion $previousFloor $floor)) {
            [PSCustomObject]@{ PackageId = $packageId; PreviousFloor = $previousFloor; Floor = $floor }
        }
    }
}

function FormatRaisedDependencyFloorsLine($RaisedFloors) {
    # The changelog line stating the floors raised across a minor version; $null when there are none.
    if (@($RaisedFloors).Count -eq 0) { return $null }
    $floors = (@($RaisedFloors) | ForEach-Object { "$($_.PackageId) $($_.Floor) (was $($_.PreviousFloor))" }) -join ', '
    return "$script:RaisedDependencyFloorsLineLead $floors. Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well."
}

function FindRaisedDependencyFloorsLine([string]$ChangelogSection) {
    # The section's line stating raised dependency floors; $null when it has none.
    return @($ChangelogSection -split "`n" | Where-Object { $_.StartsWith($script:RaisedDependencyFloorsLineLead) }) | Select-Object -First 1
}
