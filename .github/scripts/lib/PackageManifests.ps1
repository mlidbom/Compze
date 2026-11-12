# A package's manifest (its .nuspec), read from a package file or from nuget.org, and the release notes written into a
# package file's manifest. Used by the release scripts and by DevScripts. DevScripts dot-sources this file into an
# interactive PowerShell session, so nothing in it may call exit.

function ConvertFromPackageManifestXml([string]$ManifestXml) {
    # Reads a package's id, version, the commit it was built from and its Compze dependency floors, out of the text of
    # its manifest.
    #
    # The commit is the repository element's commit attribute, which the .NET SDK's source control integration stamps
    # into every package it packs, here and in the release workflow alike. It is what tells two builds of one version
    # apart. Older Compze releases predate the stamp, so RepositoryCommit may be $null.
    #
    # A dependency floor is the lowest version of a dependency that a package accepts. Compze's dependencies name a
    # version with no upper bound, so a floor of 0.7.2 means 0.7.2 or newer. CompzeDependencyFloors maps the id of each
    # Compze package this one depends on to its floor.
    [xml]$manifest = $ManifestXml
    $metadata = $manifest.package.metadata
    # Selected rather than read as properties: under Set-StrictMode a missing element or attribute read as a property throws.
    $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
    $repositoryCommit = if ($repository -and $repository.HasAttribute('commit')) { $repository.GetAttribute('commit') } else { $null }
    return [PSCustomObject]@{
        PackageId              = $metadata.id
        Version                = $metadata.version
        RepositoryCommit       = $repositoryCommit
        CompzeDependencyFloors = ReadCompzeDependencyFloors $manifest
    }
}

function ReadNupkgManifest([string]$NupkgPath) {
    # The manifest of a package file (see ConvertFromPackageManifestXml).
    $package = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
    try {
        $reader = [System.IO.StreamReader]::new((GetManifestEntry $package $NupkgPath).Open())
        try { $manifestXml = $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally {
        $package.Dispose()
    }
    return ConvertFromPackageManifestXml $manifestXml
}

function ReadNuGetOrgPackageManifest([string]$PackageId, [string]$Version) {
    # The manifest of the package nuget.org serves for one version (see ConvertFromPackageManifestXml).
    $id = $PackageId.ToLowerInvariant()
    $manifestXml = (Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$id/$($Version.ToLowerInvariant())/$id.nuspec" -TimeoutSec 30).Content
    return ConvertFromPackageManifestXml $manifestXml
}

function WriteReleaseNotesIntoNupkg([string]$NupkgPath, [string]$ReleaseNotes) {
    # Sets the release notes in a package file's manifest: what nuget.org's package page and NuGet clients show as what
    # the version changed. Fails when they are longer than the 35,000 characters nuget.org accepts, which would
    # otherwise fail the push.
    $nuGetOrgReleaseNotesLimit = 35000
    if ($ReleaseNotes.Length -gt $nuGetOrgReleaseNotesLimit) {
        throw "The release notes for $NupkgPath are $($ReleaseNotes.Length) characters long, and nuget.org accepts at most $nuGetOrgReleaseNotesLimit."
    }

    $package = [System.IO.Compression.ZipFile]::Open($NupkgPath, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $manifestStream = (GetManifestEntry $package $NupkgPath).Open()
        try {
            $manifest = [xml]::new()
            $manifest.PreserveWhitespace = $true
            $manifest.Load($manifestStream)
            SetReleaseNotesElement $manifest $ReleaseNotes

            $manifestStream.Position = 0
            $manifestStream.SetLength(0)
            $writerSettings = [System.Xml.XmlWriterSettings]::new()
            $writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
            $writer = [System.Xml.XmlWriter]::Create($manifestStream, $writerSettings)
            try { $manifest.Save($writer) } finally { $writer.Dispose() }
        } finally {
            $manifestStream.Dispose()
        }
    } finally {
        $package.Dispose()
    }
}

function GetManifestEntry([System.IO.Compression.ZipArchive]$Package, [string]$NupkgPath) {
    # The manifest is the one .nuspec at the package root.
    $manifestEntry = $Package.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
    if (-not $manifestEntry) { throw "$NupkgPath contains no .nuspec at its root, so it is not a NuGet package." }
    return $manifestEntry
}

function ReadCompzeDependencyFloors([xml]$Manifest) {
    # The floor of each Compze dependency, in every dependency group: Compze packages target one framework, so one
    # floor per dependency, which this asserts.
    $floors = [ordered]@{}
    $dependencies = $Manifest.SelectNodes("//*[local-name()='metadata']/*[local-name()='dependencies']//*[local-name()='dependency']")
    foreach ($dependency in @($dependencies | Where-Object { $_.id -like 'Compze.*' } | Sort-Object { $_.id })) {
        if ($dependency.version -match '[\[\(,]') {
            throw "$($Manifest.package.metadata.id) $($Manifest.package.metadata.version) depends on $($dependency.id) in the range '$($dependency.version)', where Compze's dependencies name a version with no upper bound."
        }
        if ($floors.Contains($dependency.id) -and $floors[$dependency.id] -ne $dependency.version) {
            throw "$($Manifest.package.metadata.id) $($Manifest.package.metadata.version) depends on $($dependency.id) with two floors, $($floors[$dependency.id]) and $($dependency.version): a package that targets several frameworks, which this does not handle yet."
        }
        $floors[$dependency.id] = $dependency.version
    }
    return $floors
}

function SetReleaseNotesElement([xml]$Manifest, [string]$ReleaseNotes) {
    $metadata = $Manifest.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    $releaseNotesElement = $metadata.SelectSingleNode("*[local-name()='releaseNotes']")
    if (-not $releaseNotesElement) {
        # Placed after the description, where the .NET SDK's pack places it, and indented like its siblings.
        $description = $metadata.SelectSingleNode("*[local-name()='description']")
        $releaseNotesElement = $Manifest.CreateElement('releaseNotes', $metadata.NamespaceURI)
        $metadata.InsertAfter($releaseNotesElement, $description) | Out-Null
        $metadata.InsertAfter($Manifest.CreateWhitespace($description.PreviousSibling.Value), $description) | Out-Null
    }
    $releaseNotesElement.InnerText = $ReleaseNotes
}
