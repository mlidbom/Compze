<#
.SYNOPSIS
    Pulls the latest shared instructions from the remote repository into this workspace.

.DESCRIPTION
    Clones the shared-instructions repo into a temp directory, copies its contents
    (excluding sync-scripts/ and .git/) over the local shared-instructions folder,
    then cleans up the temp clone.

    Changes appear as normal working-tree modifications — review and commit them yourself.
#>

$ErrorActionPreference = 'Stop'

$remote = 'https://github.com/mlidbom/copilot-code-standards-and-instructions.git'
$branch = 'main'

# sync-scripts/ → shared-instructions/ → instructions/ → .github/ → repo root
$sharedDir = $PSScriptRoot | Split-Path
$repoRoot  = $sharedDir   | Split-Path | Split-Path | Split-Path

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "shared-instructions-sync-$(Get-Random)"

try {
    Write-Host "Cloning $remote ($branch) into temp directory..."
    git clone --depth 1 --branch $branch $remote $tempDir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed (exit code $LASTEXITCODE)" }

    # Remove everything in the local shared-instructions dir (sync-scripts/ will be restored from the clone)
    Get-ChildItem $sharedDir -Force |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force }

    # Copy everything from the clone except .git/
    Get-ChildItem $tempDir -Force |
        Where-Object { $_.Name -ne '.git' } |
        ForEach-Object { Copy-Item $_.FullName -Destination $sharedDir -Recurse -Force }

    Write-Host ""
    Write-Host "NOTE: If the sync scripts themselves were updated, re-run this script to pick up the latest version."

    Write-Host "Done. Review changes with 'git diff' and commit when ready."
}
finally {
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
}
