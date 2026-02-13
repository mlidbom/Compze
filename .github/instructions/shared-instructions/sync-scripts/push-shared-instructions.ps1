<#
.SYNOPSIS
    Pushes local shared-instructions changes back to the remote repository.

.DESCRIPTION
    Clones the shared-instructions repo into a temp directory, replaces its contents
    with the local shared-instructions files (excluding sync-scripts/ and .git/),
    commits the diff, and pushes to the remote.

    Requires push access to the remote repository.
#>

$ErrorActionPreference = 'Stop'

$remote = 'https://github.com/mlidbom/copilot-code-standards-and-instructions.git'
$branch = 'main'

# sync-scripts/ → shared-instructions/ → instructions/ → .github/ → repo root
$sharedDir = $PSScriptRoot | Split-Path

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "shared-instructions-sync-$(Get-Random)"

try {
    Write-Host "Cloning $remote ($branch) into temp directory..."
    git clone --depth 1 --branch $branch $remote $tempDir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed (exit code $LASTEXITCODE)" }

    # Remove everything in the clone except .git/
    Get-ChildItem $tempDir -Force |
        Where-Object { $_.Name -ne '.git' } |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force }

    # Copy local shared-instructions into the clone
    Get-ChildItem $sharedDir -Force |
        ForEach-Object { Copy-Item $_.FullName -Destination $tempDir -Recurse -Force }

    Push-Location $tempDir
    try {
        # Inherit user identity from the main repo so commits work in shallow clones
        $userName  = git -C $sharedDir config user.name
        $userEmail = git -C $sharedDir config user.email
        if ($userName)  { git config user.name  $userName }
        if ($userEmail) { git config user.email $userEmail }

        git add --all
        $status = git status --porcelain
        if (-not $status) {
            Write-Host "No changes to push — remote is already up to date."
            return
        }

        Write-Host "Changes to push:"
        git diff --cached --stat

        git commit -m "Update shared instructions"
        if ($LASTEXITCODE -ne 0) { throw "git commit failed (exit code $LASTEXITCODE)" }

        git push origin $branch
        if ($LASTEXITCODE -ne 0) { throw "git push failed (exit code $LASTEXITCODE)" }

        Write-Host "Done. Changes pushed to $remote ($branch)."
    }
    finally { Pop-Location }
}
finally {
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
}
