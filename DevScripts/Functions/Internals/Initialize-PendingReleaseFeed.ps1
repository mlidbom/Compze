# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Initialize-PendingReleaseFeed {
    <#
    .SYNOPSIS
    Ensures this machine's pending-release feed exists and every repository on the machine restores from it; returns its folder

    .DESCRIPTION
    Registers the feed's folder (Get-PendingReleaseFeedDirectory) as a package source in the user-level NuGet.Config.
    That reaches every repository on this machine without a committed source path, which CI and cloud sessions could not
    resolve. A repository with package source mapping does not use it: an unmapped source serves no package there, and
    Compze's own nuget.config maps Compze.* to its nupkgs/ folder.

    The folder must exist for as long as it is registered, because NuGet fails every restore whose local source is
    missing. So nothing ever deletes the folder, only the packages in it.
    #>
    [CmdletBinding()]
    param()

    $sourceName = "compze-pending-release-feed"
    $feedDirectory = Get-PendingReleaseFeedDirectory
    New-Item -ItemType Directory -Path $feedDirectory -Force | Out-Null

    $userLevelNuGetConfig = if ($IsWindows) { Join-Path $env:APPDATA "NuGet/NuGet.Config" } else { Join-Path $HOME ".nuget/NuGet/NuGet.Config" }
    if (-not (Test-Path $userLevelNuGetConfig)) {
        throw "The user-level NuGet.Config ($userLevelNuGetConfig) does not exist. Any restore creates it; run one, then rerun this."
    }

    [xml]$config = Get-Content $userLevelNuGetConfig -Raw
    $isRegistered = @($config.configuration.packageSources.add | Where-Object { $_.key -eq $sourceName }).Count -gt 0
    if (-not $isRegistered) {
        dotnet nuget add source $feedDirectory --name $sourceName --configfile $userLevelNuGetConfig | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Registering the pending-release feed as package source '$sourceName' in $userLevelNuGetConfig failed." }
        Write-Host "Registered the pending-release feed ($feedDirectory) as package source '$sourceName' in ${userLevelNuGetConfig}: every repository on this machine now restores from it."
    }

    return $feedDirectory
}
