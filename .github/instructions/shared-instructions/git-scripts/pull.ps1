# Mount-agnostic: derives the repo root from git and the subtree prefix from this script's location
# (git-scripts/ sits directly under the subtree mount).
$repoRoot = git -C $PSScriptRoot rev-parse --show-toplevel
$mount = Split-Path $PSScriptRoot -Parent
$prefix = [IO.Path]::GetRelativePath($repoRoot, $mount).Replace('\', '/')

Push-Location $repoRoot
try { git subtree pull --prefix $prefix https://github.com/mlidbom/copilot-code-standards-and-instructions.git main }
finally { Pop-Location }
