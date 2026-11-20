# Native AOT: todo (found by reading, not tried)

## Status

Found 2026-11-20 by reading the code, answering whether the Claude tooling could publish a Tessaging endpoint with
Native AOT. Not tried, and nothing decided.

## What reading found

- No Compze project declares `IsAotCompatible`, so the build reports no AOT or trimming warnings.
- Code that Native AOT can't run, or can't run without help:
  - `MakeGenericType` in `PublisherTevent`, `SharedTomponentSlot` and the generics of Compze.TypeIdentifiers.
  - Expression `Compile` of constructors, and `Emit`, in Compze.Internals.SystemCE.
  - Newtonsoft.Json, which serializes the tessages: it builds its contracts by reflection, and with
    `TypeNameHandling.Auto` (`RenamingAndNonPublicMembersSupportingJsonSettings`) it finds types by name at run time.

## Meanwhile

A self-contained publish works without AOT. To make starting faster, measure ReadyToRun first.

## To do, if AOT is wanted

Set `IsAotCompatible` on the libraries, and work through the warnings the build then reports.
