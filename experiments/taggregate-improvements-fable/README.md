# Experiments for the Taggregate improvements (the Fable session)

Experiments behind the documents in `dev_docs/taggregate-improvements/fable` on the branch `taggregate-improvements`.
Nothing here changes Compze: each program is built by itself, against Compze.Teventive 0.6.0-alpha from nuget.org.
The results, and what they mean, are in those documents: `what-was-run-and-measured.md`.

| Folder | What it is |
|---|---|
| `TaggregateCost` | The JAStudio session's measurement program, as it wrote it: what today's Taggregate costs for each instance. |
| `ClaimsCheckedByRunning` | Runs what the documents first claimed from reading the code: the two bugs, what an exception leaves behind, loading a tevent constructed outside `Publish`, a slot in a component, what a subscriber can subscribe to. |
| `TeventRoutingPerClassPrototype` | A prototype of tevent routing decided once for each class, with the small additions to `Taggregate` (`TeventRoutingPerClass.cs`); the same vocab written against today's Taggregate and against the prototype; a program that measures the two side by side, and runs the behaviour the proposal says the prototype has. |

## Running one

```
cd experiments/taggregate-improvements-fable/TeventRoutingPerClassPrototype
dotnet run -c Release
```

The bytes are the same in every run. The times move with what else the machine is doing.

## Why this folder has build files of its own

`Directory.Build.props`, `Directory.Build.targets` and `nuget.config` here stop the repository's own from applying:
FlexRef, and the local package feed, which a fresh worktree doesn't have. The programs then build as a consumer's
would, from nuget.org.
