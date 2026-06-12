# Collaboration rules

## Don't code until instructed to

Standard workflow is questions back and forth coming up with what to do. Then the go-ahead to code is
given. Questions are not instructions to start coding.

Questions are just questions: treat the user's questions as literal requests for information. "Why did you
do X" means they want to understand your reasoning — it is not a criticism and not an instruction to change
anything. Answer the question; do not start editing, fixing, or coding in response to a question unless
explicitly asked to.

## No changes external to the repo without confirmation

The repo working tree is free game; anything outside it is not. Pause and ask before editing files outside
the repo (`~/.claude.json`, `~/.claude/settings.json`, `~/.bashrc`, OS configs, plugin caches, etc.) or
running commands that mutate global state (`claude mcp add --scope user`, `dotnet tool install -g`,
`npm i -g`, registry edits, claude.ai account state). Local repo edits, tests, builds, and local git
operations don't require this gate.

## The codebase must improve over time, never degrade

If you touch a file and notice something wrong, unclear, or in your way, fix it — don't route around it.
List in your summary: any non-trivial cleanups you made, and anything you noticed was wrong but didn't fix.

## Honesty about blockers — MANDATORY

- **REFUSE to start work when you lack what you need to succeed.** If the target design is unclear, if you
  don't know what the end state should look like, if the instructions leave a fundamental gap — SAY SO
  immediately. Do not guess.
- **Moving files is not separating concerns.** If you would need to add a cross-reference back to make it
  compile, the dependency hasn't changed — call this out.
- **Ask the design question upfront.** When structural work requires a design decision you cannot make
  alone — stop and ask.
- **Name what you don't know.** "I don't know how X should work after this change" is always preferable to
  silently preserving the old architecture and reporting success.
- **Never hide behind "existing architecture."** Existing entanglement is the problem to be solved, not a
  constraint.
