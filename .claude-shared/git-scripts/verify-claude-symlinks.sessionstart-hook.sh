#!/bin/sh
# SessionStart adapter for the symlink verifier. The verifier reports failures on stderr + non-zero exit —
# correct for a build/CI gate, but a Claude Code SessionStart hook injects into the agent's context ONLY via
# STDOUT on exit 0; stderr on a non-zero exit reaches the user, never the agent, and cannot block the session.
# So run the verifier and, on failure, re-emit its message on stdout (with agent-directed framing) and exit 0,
# so the warning actually lands in the agent's context at session start.
dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if report=$("$dir/verify-claude-symlinks.sh" 2>&1); then
   exit 0
fi
printf '%s\n%s\n' \
  'STOP: this checkout is degraded — symlinked .claude rules/skills have materialized as plain text files. Do NOT trust loaded rules or skills; tell Magnus immediately and surface the message below.' \
  "$report"
