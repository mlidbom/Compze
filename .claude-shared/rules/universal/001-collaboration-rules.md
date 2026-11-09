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

## Build the tools the work will need again now, not later — and propose the big ones

When a task needs something the work will need again and again — an analysis, a diff, a check, a way to see
or drive the system — build the tool for it as part of the task. Don't do it by hand, or with a throwaway
script, and promise the tool for later. A tool pays for itself from its second use; until it exists, every
use pays the manual cost again, and "later" rarely comes. Faster long-term progress outranks finishing the
current task a little sooner.

- Writing a throwaway script for the second time is the signal: it's a tool, so build it properly.
- The same goes for improving a tool that falls short.
- Say in the summary which tool you built or changed, and why.

**Think big too.** Don't stop at small additions to the existing infrastructure. Keep asking what would make
development as a whole much faster, and when the answer is a major change — new infrastructure, another way
of running or reaching the system, a restructuring — propose it, unasked, however much bigger than the task
at hand. Propose it and agree it first: a major change is never started on your own.

**Tooling for AI agents counts as much as tooling for humans, and is the most often missed:** whatever lets an
agent see, drive, test and verify the system itself, without a round trip through the human. Look for the
steps only the human can do now — restart the app, run the rebuild, look at the screen, paste the output.
Each is a candidate for a tool. JAStudio's MCP server is the example: once built, in about a day, the loop of
editing the corpus, rebuilding and analyzing that had waited on the human each time ran in seconds without
him. An agent should have proposed it long before he did.

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
