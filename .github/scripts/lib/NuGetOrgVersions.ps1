# The one implementation of "which versions of a package does nuget.org have": used by the release summary
# (SummaryReport.ps1) and by DevScripts (Get-NuGetOrgRestorableVersions). DevScripts dot-sources this file into an
# interactive PowerShell session, so nothing in it may call exit.

function GetNuGetOrgRestorableVersionsMap($PackageNames) {
    # Returns a hashtable: package name -> the versions a restore can download from nuget.org right now, normalized,
    # lowercase and in ascending semver order (as nuget.org returns them); an empty array for a package nuget.org lacks.
    #
    # Reads nuget.org's package content index (the v3 flat container), which is where restore itself downloads from.
    # A freshly pushed version appears there only once nuget.org has validated and indexed it: typically 5 to 20 minutes
    # after the push, and documented as up to an hour.
    #
    # Fails when nuget.org can't be reached or answers with an error, rather than reporting the package as unpublished.
    # Queries in parallel.
    $results = $PackageNames | Sort-Object -Unique | ForEach-Object -Parallel {
        $ProgressPreference = 'SilentlyContinue'
        $name = $_
        # -SkipHttpErrorCheck: a never-published package answers 404, which is an answer, not a failure.
        $response = Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$($name.ToLowerInvariant())/index.json" -SkipHttpErrorCheck -TimeoutSec 30
        if ($response.StatusCode -eq 404) { return [PSCustomObject]@{ Name = $name; Versions = @() } }
        if ($response.StatusCode -ne 200) { throw "nuget.org returned $($response.StatusCode) for $name" }
        [PSCustomObject]@{ Name = $name; Versions = @(($response.Content | ConvertFrom-Json).versions) }
    } -ThrottleLimit 20

    $versionsByPackageName = @{}
    foreach ($result in $results) { $versionsByPackageName[$result.Name] = $result.Versions }
    return $versionsByPackageName
}
