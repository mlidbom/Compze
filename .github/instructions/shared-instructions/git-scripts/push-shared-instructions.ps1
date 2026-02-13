# git-scripts/ → shared-instructions/ → instructions/ → .github/ → repo root
$repoRoot = $PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path
$remote = 'https://github.com/mlidbom/copilot-code-standards-and-instructions.git'
$prefix = '.github/instructions/shared-instructions'

Push-Location $repoRoot
try {
   # Fix false "working tree has modifications" caused by stat-dirty index entries
   git update-index --refresh -q 2>$null

   # Fetch remote so split can resolve squashed commit references
   git fetch $remote main
   if ($LASTEXITCODE -ne 0) { throw "git fetch failed (exit code $LASTEXITCODE)" }

   # Split with --rejoin caches the split point so future pushes only process new commits.
   # Without this, git subtree push walks the entire repo history every time.
   $branch = git subtree split --prefix $prefix --rejoin
   if ($LASTEXITCODE -ne 0) { throw "git subtree split failed (exit code $LASTEXITCODE)" }

   git push $remote "${branch}:main"
   if ($LASTEXITCODE -ne 0) { throw "git push failed (exit code $LASTEXITCODE)" }
}
finally { Pop-Location }
