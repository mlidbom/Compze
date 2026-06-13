# Verifies that every symlink the committed tree (HEAD) carries under .claude/ is a real symlink in the
# working tree that resolves to an existing target.
#
# Guards against the silent Windows failure mode: a checkout with core.symlinks=false materializes
# committed symlinks as plain text files containing the target path — which Claude Code would then
# load as garbage rules/skills without complaint.
#
# Silent on success; throws with fix instructions on failure.
$repoRoot = git -C $PSScriptRoot rev-parse --show-toplevel

# The committed tree (HEAD) — not the index — is the source of truth for which paths are symlinks. A degraded
# checkout can leave the index ALSO recording a path as a plain file (mode 100644, once the materialized plain
# file is staged); reading the index would then drop that path from the checklist and miss the very corruption
# we exist to catch.
$symlinkPaths = git -C $repoRoot ls-tree -r HEAD -- .claude |
   Where-Object { $_ -match '^120000\s' } |
   ForEach-Object { ($_ -split "`t", 2)[1] }

$broken = @()
foreach ($relativePath in $symlinkPaths) {
   $fullPath = Join-Path $repoRoot $relativePath
   $item = Get-Item $fullPath -Force -ErrorAction SilentlyContinue
   if (-not $item -or $item.LinkType -ne 'SymbolicLink') {
      $broken += "$relativePath — git tracks this as a symlink, but the working tree has a plain file/directory"
      continue
   }
   $target = $item.ResolveLinkTarget($true)
   if (-not $target -or -not $target.Exists) {
      $broken += "$relativePath — symlink does not resolve (target: $($item.Target))"
   }
}

if ($broken.Count -gt 0) {
   throw @"
Claude config symlinks are broken:
$($broken -join "`n")

Fix:
  1. Turn on Windows Developer Mode (Settings > System > For developers) so symlinks can be created without elevation.
  2. git config core.symlinks true     (add --global to cover future clones)
  3. Restore the links: git restore --source=HEAD --staged --worktree -- .claude
"@
}
