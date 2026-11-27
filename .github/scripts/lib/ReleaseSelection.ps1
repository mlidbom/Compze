function GetPublishablePackages($Packages) {
    # Convention: packages with version 0.0.0-not-published-to-nuget are packable for local consumption only — never publish to NuGet
    return @($Packages | Where-Object { $_.Version -ne '0.0.0-not-published-to-nuget' })
}

function GetPackagesToRelease($PublishablePackages) {
    # The packages a release publishes: the versions with no release tag yet, each with its changelog section.
    # The release workflow and C-Publish-ToPendingReleaseFeed both select through here, so a version published
    # locally ahead of nuget.org is exactly one the release will publish.
    # Exits 1 when one of them has no changelog entry for its version (see GetAllReleaseDetails), and when the packages'
    # exact-version dependencies refuse the release (see GetExactVersionDependencyFailures).
    $packagesToRelease = @(GetAllReleaseDetails @(PackagesWithNoMatchingReleaseTag $PublishablePackages))
    $exactVersionDependencyFailures = @(GetExactVersionDependencyFailures $PublishablePackages @($packagesToRelease | ForEach-Object { $_.PackageName }))
    if ($exactVersionDependencyFailures.Count -gt 0) {
        Write-Host "`n::error::Exact-version dependency validation failed:" -ForegroundColor Red
        foreach ($failure in $exactVersionDependencyFailures) { Write-Host "  - $failure" -ForegroundColor Red }
        exit 1
    }
    return $packagesToRelease
}
