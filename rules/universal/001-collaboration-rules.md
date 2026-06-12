# Collaboration rules

## Don't code until instructed to

Standard workflow is questions back and forth coming up with what to do. Then the go-ahead to code is
given. Questions are not instructions to start coding.

This gates *starting* to code while a discussion is still going — it does not narrow coding already under
way: once the go-ahead is given, "the codebase must improve" below applies to everything you touch.

Questions are just questions: treat the user's questions as literal requests for information. "Why did you
do X" means they want to understand your reasoning — it is not a criticism and not an instruction to change
anything. Answer the question; do not start editing, fixing, or coding in response to a question unless
explicitly asked to.

## The codebase must improve over time, never degrade

If you touch a file and notice something wrong, unclear, or in your way, fix it — don't route around it.
List in your summary: any non-trivial cleanups you made, and anything you noticed was wrong but didn't fix.

## When given full delegation ("use your own judgment, keep going until it is actually good")

Take it literally: make the design calls; work in increments that each build clean and pass the full suite;
commit each increment with a commit message that records the *why*; record decisions and the as-built design in
the repo's docs as you go; and collect taste/naming questions into a follow-ups list to surface at the end
instead of stopping to ask. Optimize for a design a human can divide and conquer mentally — intuitive
object-oriented units — never merely for "works".

## Honesty about blockers — MANDATORY

- **REFUSE to start work when you lack what you need to succeed.** If the target design is unclear, if you
  don't know what the end state should look like, if the instructions leave a fundamental gap — SAY SO
  immediately. Do not guess.
- **Ask the design question upfront.** When structural work requires a design decision you cannot make
  alone — stop and ask.
- **Name what you don't know.** "I don't know how X should work after this change" is always preferable to
  silently preserving the old architecture and reporting success.
- **Never hide behind "existing architecture."** Existing entanglement is the problem to be solved, not a
  constraint.
