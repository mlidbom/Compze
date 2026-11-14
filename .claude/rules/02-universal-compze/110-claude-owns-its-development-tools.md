# Claude owns the tools it develops Compze with: improve them proactively, without asking

**The tools through which Claude builds, runs, sees, drives and verifies Compze without Magnus are Claude's, as a
product.** Magnus (2026-11-14): "consider yourself the product owner for dev-tools that you need". They are this
project's case of the collaboration rule "Build the tools the work will need again now, not later — and propose the big
ones". Today they are:

- the across-machines lab: the `C-VM-*` commands, the lab program (`test/Compze.Tests.AcrossMachines.Lab`), and the lab
  machine with its checkpoint and its folder (`C:\CompzeVMLab`);
- Hyper-V on this computer: all its configuration, creating and deleting virtual machines included, and the changes to
  the computer that the virtual machines need, such as the firewall rule that lets them in. The one limit: nothing is
  messed up for Deskmancer, which has its own virtual machines there. Magnus: "All hyper-v config and changes you need
  are yours. As long as you don't mess up things for deskmancer." And: "that includes creating, and deleting vms";
- the specification harness, as distinct from what the specifications claim: `Compze.Tests.Infrastructure`, and the
  endpoint host process of the multi-process specifications (`test/Compze.Tests.SameMachine.EndpointHostProcess`);
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

**Pushing is always fine, whatever the branch:** to have CI run the full matrix, or for any other reason. Magnus
(2026-11-14): "Always OK regardless of branch. Nothing push does is irreversible." A release is not a push: the rule on
releasing holds.

**The DevScripts commands that Magnus and others run,** such as `C-Build`, `C-Test` and those for packing, releasing and
managing projects, are free to add to: a new command, a new parameter, more output behind a switch. What a command does
by default changes only after asking him, such as making one more verbose.

**What the ownership covers:** the tools themselves, meaning what they do, how they're run and how they report, and the
code behind them, wherever that code has to live. **What it doesn't cover:**

- what Compze's libraries do, and their public API. A tool that would need such a change is normal work, discussed with
  Magnus first;
- what the specifications claim about Compze. Adding or changing a specification is normal work;
- what the rule on external environments covers, other than pushing.

The tools are part of Compze's code, so the code standards and the DevScripts conventions apply to them.
