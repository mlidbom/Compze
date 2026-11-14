function Get-TargetedDotNetChannel {
    <#
    .SYNOPSIS
    The channel of the .NET version the repository's projects target, such as 11.0

    .DESCRIPTION
    src/Directory.Build.props sets the target framework for every project, such as net11.0. The channel is its version:
    what a .NET runtime's folder name starts with, and what the lab machine's guest must have to run what is built here.
    #>
    $buildProperties = Join-Path $script:CompzeRoot 'src\Directory.Build.props'
    $targetFramework = [regex]::Match((Get-Content -LiteralPath $buildProperties -Raw), '<TargetFramework>net(\d+\.\d+)</TargetFramework>')
    if (-not $targetFramework.Success) { throw "$buildProperties sets no target framework of the form net<major>.<minor>." }
    $targetFramework.Groups[1].Value
}
