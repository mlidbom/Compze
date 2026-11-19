#!/bin/sh
# SessionStart hook: makes sure this machine has its clone of the shared Claude config, and hands over to the
# catalog's own session-start hook, which attaches the catalog to this checkout, brings it up to date, and verifies
# every .claude/ symlink into it.
#
# A copy of this file lives in each consuming repository at .claude/attach-shared-claude-config.sessionstart-hook.sh;
# the canonical version is in the catalog under git-scripts/copy-into-consuming-repository/. Until 2026-11-19 it was
# named verify-shared-claude-config.sessionstart-hook.sh. Each machine has one clone of the catalog, at
# ~/.claude/.claude-shared, and each other checkout's .claude-shared is a git worktree of it. The catalog's own scripts
# do all of that but make the clone, which holds them: this file clones it when the machine has none, as in a cloud
# session or on a new machine.
#
# A SessionStart hook reaches the agent only through stdout on exit 0, so the warning goes to stdout.
checkout=$(git -C "$(dirname -- "$0")" rev-parse --show-toplevel)
machine_clone="$HOME/.claude/.claude-shared"
catalog_hook="$machine_clone/git-scripts/sessionstart-hook.sh"
if [ ! -f "$catalog_hook" ]; then
   # No prompt for credentials: nobody is there to answer one. Bounded where `timeout` exists (Git Bash, Linux), so a
   # connection that hangs doesn't hold up the session's start.
   clone='git clone --quiet https://github.com/mlidbom/copilot-code-standards-and-instructions.git'
   if command -v timeout >/dev/null 2>&1; then clone="timeout 60 $clone"; fi
   GIT_TERMINAL_PROMPT=0 $clone "$machine_clone" >/dev/null 2>&1
   # Another session's hook may have cloned it at the same moment, so what is checked is the clone, not the exit code.
   if [ ! -f "$catalog_hook" ]; then
      echo "STOP: the shared rules and skills are missing: this machine has no clone of the shared Claude config at $machine_clone, and cloning it failed. Do NOT trust loaded rules or skills. Tell Magnus."
      exit 0
   fi
   export SHARED_CLAUDE_CONFIG_WAS_JUST_CLONED=yes
fi
exec sh "$catalog_hook" "$checkout"
