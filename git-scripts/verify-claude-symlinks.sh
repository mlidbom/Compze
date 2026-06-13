#!/bin/sh
# Verifies that every symlink git tracks under .claude/ is a real symlink in the working tree that resolves
# to an existing target.
#
# Guards against the silent failure mode where a checkout with core.symlinks=false (or no Developer Mode on
# Windows) materializes committed symlinks as plain text files containing the target path — which Claude Code
# would then load as garbage rules/skills without complaint.
#
# POSIX sh + git only, no PowerShell dependency, so it runs as a SessionStart hook on Windows (Git Bash),
# Linux, and macOS. Mount-agnostic: derives the repo root from git via this script's own location. Silent on
# success; exits non-zero with fix instructions on stderr on failure. (PowerShell twin for build scripts:
# verify-claude-symlinks.ps1.)
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repo=$(git -C "$script_dir" rev-parse --show-toplevel)

# git ls-files -s prints: "<mode> <sha> <stage>\t<path>" — symlinks are mode 120000; the path follows a TAB.
symlinks=$(git -C "$repo" ls-files -s -- .claude | grep '^120000 ' | cut -f2-)

broken=''
while IFS= read -r path; do
   [ -n "$path" ] || continue
   full="$repo/$path"
   if [ ! -L "$full" ]; then
      broken="${broken}  ${path} — git tracks this as a symlink, but the working tree has a plain file/directory
"
   elif [ ! -e "$full" ]; then
      broken="${broken}  ${path} — symlink does not resolve
"
   fi
done <<EOF
$symlinks
EOF

[ -z "$broken" ] && exit 0

cat >&2 <<EOF
Claude config symlinks are broken:
$broken
Fix:
  1. Turn on Windows Developer Mode (Settings > System > For developers) so symlinks can be created without elevation.
  2. git config core.symlinks true     (add --global to cover future clones)
  3. Restore the links: git checkout -- .claude
EOF
# Exit 2 so a SessionStart hook surfaces the stderr message instead of failing silently.
exit 2
