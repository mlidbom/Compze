# Creates/refreshes the directory junctions that bring the co-located _docs documentation into the website's
# DocFX content cone. DocFX only processes content located under the folder containing docfx.json, so each
# Compze project that carries public _docs documentation is junctioned in as Compze\<name-without-Compze-prefix>,
# e.g. Compze\Teventive -> ..\..\Compze.Teventive. The Compze folder is git-ignored; junctions are per-clone.
#
# Idempotent: creates missing junctions, retargets wrong ones, removes stale ones. Runs automatically before
# every docfx build via the npm pre-hooks in package.json and from buildAndPublish.ps1; safe to run by hand.
$ErrorActionPreference = 'Stop'

$websiteFolder = $PSScriptRoot
$sourceFolder = (Resolve-Path (Join-Path $websiteFolder '..\..')).Path
$junctionRoot = Join-Path $websiteFolder 'Compze'

$projectsWithPublicDocs = Get-ChildItem -Path $sourceFolder -Directory -Filter 'Compze.*' |
   Where-Object { Get-ChildItem -Path $_.FullName -Recurse -Directory -Filter '_docs' -ErrorAction SilentlyContinue | Select-Object -First 1 }

New-Item -ItemType Directory -Force $junctionRoot | Out-Null

$desiredJunctions = @{}
foreach($project in $projectsWithPublicDocs)
{
   $junctionName = $project.Name.Substring('Compze.'.Length)
   $desiredJunctions[$junctionName] = $project.FullName
}

foreach($existingJunction in Get-ChildItem -Path $junctionRoot -Directory -ErrorAction SilentlyContinue)
{
   $isStale = -not $desiredJunctions.ContainsKey($existingJunction.Name)
   $isWrong = -not $isStale -and ($existingJunction.LinkType -ne 'Junction' -or $existingJunction.LinkTarget -ne $desiredJunctions[$existingJunction.Name])
   if($isStale -or $isWrong)
   {
      $existingJunction.Delete() # DirectoryInfo.Delete removes only the junction's reparse point, never the target's contents.
      Write-Host "Removed $(if($isStale) { 'stale' } else { 'mistargeted' }) junction: Compze\$($existingJunction.Name)"
   }
}

foreach($desiredJunction in $desiredJunctions.GetEnumerator())
{
   $junctionPath = Join-Path $junctionRoot $desiredJunction.Key
   if(-not (Test-Path $junctionPath))
   {
      New-Item -ItemType Junction -Path $junctionPath -Target $desiredJunction.Value | Out-Null
      Write-Host "Created junction: Compze\$($desiredJunction.Key) -> $($desiredJunction.Value)"
   }
}
