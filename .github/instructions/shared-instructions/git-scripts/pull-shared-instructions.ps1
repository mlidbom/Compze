# git-scripts/ → shared-instructions/ → instructions/ → .github/ → repo root
$repoRoot = $PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path
Push-Location $repoRoot
try { git subtree pull --prefix .github/instructions/shared-instructions https://github.com/mlidbom/copilot-code-standards-and-instructions.git main }
finally { Pop-Location }
