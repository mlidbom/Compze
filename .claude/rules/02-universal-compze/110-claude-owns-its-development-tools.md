# Claude owns the tools it develops Compze with: improve them proactively, without asking

**The tools through which Claude builds, runs, sees, drives and verifies Compze without Magnus are Claude's, as a
product.** Magnus (2026-11-14): "consider yourself the product owner for dev-tools that you need". They are this
project's case of the collaboration rule "Build the tools the work will need again now, not later — and propose the big
ones". Today they are:

- the across-machines lab: the `C-VM-*` commands, the lab program (`test/Compze.Tests.AcrossMachines.Lab`), and the lab
  machine with its checkpoint and its folder (`C:\CompzeVMLab`);
- `C-Test-Combinations`, which runs the built tests against other pluggable-component combinations than those in
  Magnus's own combinations file;
- the instruments for hunting flakes that the rule on diagnosing CI flakiness describes: the flake-hunt workflow,
  `WaitAsyncDumpingTheProcessOnTimeout`, and what CI prints about load and dumps;
- the docs that describe all of the above.

What Claude adds to develop or verify its own work joins the list.

- **Be proactive.** Whenever a task would go faster or better with a tool that doesn't exist, or with an existing one
  changed, add or change it then, as part of the work. Don't work around a missing tool with throwaway scripts, repeated
  manual steps or guesses: that's the signal to improve the tools. Removing or redesigning tools that turn out unhelpful
  is just as much part of owning them.
- **No permission needed.** Don't ask before adding to or changing these tools. Tell Magnus afterwards, in a line or two,
  what you added or changed and why you needed it.
- **Commit it separately, whenever it suits the work.** Changes to these tools go in commits of their own, apart from the
  work that needed them, so each kind of change is easy to follow. They need no go-ahead from Magnus to be committed. The
  usual checks still come first: the build has no warnings, and what the change affects has been run.

**What the ownership covers:** the tools themselves, meaning what they do, how they're run and how they report, and the
code behind them, wherever that code has to live. **What it doesn't cover:**

- what Compze's libraries do, and their public API. A tool that would need such a change is normal work, discussed with
  Magnus first;
- what the specifications claim about Compze. Adding or changing a specification is normal work;
- the interface of a command that Magnus runs, or anyone else who works on Compze: `C-Build`, `C-Test`, and the
  DevScripts commands for packing, releasing and managing projects. How they're run and what they write is agreed with
  Magnus. What goes on inside them is yours to improve;
- anything outside the repository and the lab: this computer's own settings, such as its firewall, and whatever the rule
  on external environments covers, such as pushing a flake-hunt branch. Those change on Magnus's word.

The tools are part of Compze's code, so the code standards and the DevScripts conventions apply to them.
