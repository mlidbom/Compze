#!/bin/sh
# SessionStart hook: confirms the shared Claude config this repository adopts is actually present before the agent
# trusts any rule or skill from it.
#
# A copy of this file lives in each consuming repository at .claude/verify-shared-claude-config.sessionstart-hook.sh;
# the canonical version is in the catalog under git-scripts/copy-into-consuming-repository/. It cannot live in the
# catalog alone: a clone made without --recurse-submodules, or a new worktree, leaves the .claude-shared submodule
# empty, and a check stored inside the empty submodule could not run to report that. Once the submodule is present,
# the catalog's own adapter takes over and verifies every .claude/ symlink into it.
#
# A SessionStart hook reaches the agent only through stdout on exit 0, so the warning goes to stdout.
repo=$(git -C "$(dirname -- "$0")" rev-parse --show-toplevel)
catalog_hook="$repo/.claude-shared/git-scripts/verify-claude-symlinks.sessionstart-hook.sh"
if [ ! -f "$catalog_hook" ]; then
   echo 'STOP: the .claude-shared submodule is not initialized, so the shared rules and skills are missing and every .claude/ symlink into it dangles. Do NOT trust loaded rules or skills. Run  git submodule update --init  in the repository root, then restart the session. Tell Magnus.'
   exit 0
fi
exec sh "$catalog_hook"
