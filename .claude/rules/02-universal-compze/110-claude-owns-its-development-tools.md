# The development tools that are Claude's in Compze

What owning them means is in the collaboration rules: "The tools you develop with are yours, as a product", under
[Build the tools the work will need again now, not later](../01-universal-shared/001-collaboration-rules.md). In Compze
they are:

- the across-machines lab: the `C-VM-*` commands, the lab program (`test/Compze.Tests.AcrossMachines.Lab`), and the lab
  machine with its checkpoint and its folder (`C:\CompzeVMLab`);
- Hyper-V on this computer: all its configuration, creating and deleting virtual machines included, and the changes to
  the computer that the virtual machines need, such as the firewall rule that lets them in. The one limit: nothing is
  messed up for Deskmancer, which has its own virtual machines there. Magnus (2026-11-14): "All hyper-v config and
  changes you need are yours. As long as you don't mess up things for deskmancer." And: "that includes creating, and
  deleting vms";
- the specification harness, as distinct from what the specifications claim: `Compze.Tests.Infrastructure`, and the
  endpoint host process of the multi-process specifications (`test/Compze.Tests.SameMachine.EndpointHostProcess`);
- `C-Test-Combinations`, which runs the built tests against other pluggable-component combinations than those in
  Magnus's own combinations file;
- the instruments for hunting flakes that the rule on diagnosing CI flakiness describes: the flake-hunt workflow,
  `WaitAsyncDumpingTheProcessOnTimeout`, and what CI prints about load and dumps;
- the docs that describe all of the above.

What Claude adds to develop or verify its own work joins the list.

**The tools Magnus runs himself** are the other DevScripts commands, such as `C-Build`, `C-Test` and those for packing,
releasing and managing projects.
