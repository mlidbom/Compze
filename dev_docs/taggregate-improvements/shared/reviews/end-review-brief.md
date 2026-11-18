# The end review of Compze's taggregate-store spike: every public API change, and the store design

You are one of two independent reviewers, one Opus and one Fable. You don't see each other's work, and neither of you
sees the builders' conclusions until you have written your own. jastudio-rep-opus, the Compze session that built most
of the spike, consolidates both reviews for Magnus, who owns Compze and decides. The value of your review is a view
that isn't anchored on the builders': form it from the code.

## What the spike is

Compze (`C:\Dev\Compze`, .NET 11) is Magnus's set of libraries. In Compze.Teventive, a taggregate is an aggregate whose
state changes only by applying tevents, Compze's type-routed events; Compze.Teventive.TeventStore stores taggregates as
their tevents. The spike, branch `taggregate-store-spike` (now at 46cc881a6), adds a second kind of store, the state
store (`src/Compze.Teventive.StateStore`, with `src/Compze.Teventive.StateStore.MemoryPack`), which stores each
taggregate's state as a snapshot instead of its history. It was built for JAStudio, an application for learning
Japanese, whose 80,000 notes become taggregates. Along the way the spike changed Teventive itself: a reader and updater
both stores implement, taggregate-references, indexes, snapshots, tevent routing declared per class in tables, base
classes the query models share with taggregates, and observation of tevents at run time.

JAStudio is the first consumer, and already uses the spike's API on its `main`: `C:\Dev\JAStudio`, .NET code under
`src/src_dotnet/` (`JAStudio.Core/AppBootStrapper.cs` composes the store).

## Magnus's words, which set the review's questions

> "we need to go through all public API changes very carefully at the end of this with an eye towards whether this is
> good API design or kitchen sink style types and/or methods crammed in to make JAStudio's life a bit easier, or even
> just to prevent refactorings in JAStudio that would actually result in better code than the shortcuts our added stuff
> gave them." (2026-11-16)

Not only whether each type and member should exist, but whether its shape and its guarantees are what they should be
(Magnus, 2026-11-17). Its shape: the signature, the parameter and return types, where it lives, which interface it is
on. Its guarantees: what it promises a caller, such as an order, what a unit of work sees, when it loads, what it costs
at each call, what it does when it fails.

> "Is the state store designed for an application that keeps its whole database in memory? If so that does not make the
> need or the implementation bad, but it means that we should consider things like naming and/or whether it should be a
> specific mode for a store that does not always work like that. For instance, I would assume that having all-in-memory
> indexes would not play well unless all the objects are loaded." (2026-11-17)

> "I absolutely want that on the public API, with the new rules in front of mind. Both opus and fable." (2026-11-17)

## What you review

### 1. Every public API change

- **The list:** `C:\Users\magnu\AppData\Local\Temp\jastudio-rep-opus\public-api-changes-of-the-spike.md`, made by
  comparing the built assemblies of the spike's merge-base with `main` (e29fd6673) and of the spike. It is a list of
  facts, with no judgement in it. If it is missing something, say so.
- **The source:** `git -C C:\Dev\Compze diff main...taggregate-store-spike -- src`. Public means everything code
  outside the repository can compile against, protected members of public unsealed classes included.
- **How each is meant to be used:** the specifications the spike added or changed, in `test/Compze.Tests.Unit/CQRS`,
  `test/Compze.Tests.Integration/CQRS` and the others the diff lists, and the sample in `samples/AccountManagement`.
- **How JAStudio uses it:** search `C:\Dev\JAStudio\src\src_dotnet` for each.

For each public type and member the spike added or changed:

- **Would Compze have it for any consumer, as an abstraction of its own?** Or does it exist because it spared JAStudio
  a refactoring, and would JAStudio's code be better had it made that refactoring instead? Look at JAStudio's use of
  it to judge.
- **Its shape:** signature, parameter and return types, namespace and assembly, which interface it is on, interface or
  class.
- **Its guarantees:** what it promises a caller, and whether its documentation says so truly.
- **Its name:** does it tell the truth, in Compze's language?
- **Completeness:** what a consumer would need for it to be complete and unsurprising, and what could go internal or go.

### 2. The all-in-memory question

Weigh Magnus's question above: what it means for the state store's name, its public surface and its documentation, and
whether an all-in-memory store is a mode of something more general or a store of its own.

### 3. The store design as a whole

It has had no independent review beyond one of isolation between units of work. The topics: taggregate-references,
indexes defined by state, the storage seam between the store and where it keeps its data, dispatching tevents
synchronously, the tevent store taking the same internals as the state store, and the bulk reader. The design is
described in the documents below; where they and the code differ, the code is what was built, and the difference is a
finding.

## The rules to hold it to

- **Compze's strategy rules,** in `C:\Dev\Compze.worktrees\taggregate-docs\.claude\rules\02-universal-compze\01-strategy\`
  (read them there: 011 and 013 are newer than the spike's checkout): 001 (only what improves on the BCL), 010 (minimal
  visibility), 011 (the frameworks keep their public surface minimal), 012 (public types are designed for every
  consumer), 013 (an API spends nothing on what callers can do as well themselves), 015 (interfaces over classes), 020
  (`_internal` and `_private` namespaces). Read the `.rationale.txt` beside a rule where it has one.
- **The code standards,** in `C:\Dev\Compze\.claude-shared\rules\universal\code-standards\`: 007 and 010 on naming and
  the ubiquitous language, 030, 031, 040 and 051 on design.
- **From `C:\Dev\Compze\.claude-shared\rules\universal\004-compze-is-our-code.md`,** the paragraph that begins
  "Recommend what Compze would offer every consumer".

## Context

- **The design documents,** in `C:\Dev\Compze.worktrees\taggregate-docs\dev_docs\taggregate-improvements\shared\`:
  `the-taggregate-store.md` (the store's design), `the-design.md`, `observing-tevents-at-runtime.md`,
  `step-5-the-rest-of-the-teventives.md`, `snapshots-of-every-taggregate-shape.md`.
- **`README.md` in the same folder, its table under "What JAStudio asked for":** its first three columns say what
  JAStudio asked for and why. Its last column holds the builders' views: read it only after writing your own verdicts,
  then say where you disagree.
- **Not until your verdicts are written:** the `reviews/` folder (earlier reviews of earlier designs), and
  `bugs-and-findings.md` (the builders' findings).

## How to work

- **Read only, in `C:\Dev\Compze` and in JAStudio's checkouts:** other sessions work in them. Don't build in them, and
  don't change their files. Rider's code-intelligence MCP (`dotnet-analysis-and-refactoring`, with
  `solutionName: 'Compze.AllProjects'`) is fine for navigation and finding usages, and the sherlock MCP for the built
  assemblies.
- **To verify a claim by running code,** make a worktree of your own:
  `git -C C:\Dev\Compze worktree add C:\Dev\Compze.worktrees\end-review-<opus or fable> -b end-review-<opus or fable> taggregate-store-spike`.
  Build and run there (`dotnet build` of a project, `dotnet test` of a test project with `--filter`), commit what
  demonstrates a finding, and push the branch. A finding shown by running code is worth more than one judged by
  reading; say which each is.
- **Don't message other sessions:** the review must be independent.
- **Don't fix anything in Compze:** this is a review. Propose.

## What to write

Write your review to `C:\Dev\Compze.worktrees\taggregate-docs\dev_docs\taggregate-improvements\shared\reviews\<file>`
(the file name is in your prompt). Don't commit it: jastudio-rep-opus does. In this order:

1. **The headline:** the findings that matter most, most important first, a few lines each.
2. **Every public API change,** grouped by assembly and namespace. For each type, and for its members where they
   differ: a verdict (keep / keep with another shape / rename / make internal / remove / replace), the reason, a
   `file:line`, and, for what you would remove or make internal, what a consumer such as JAStudio does instead and what
   that costs it. A line is enough where a change is plainly fine, but every change gets one.
3. **The all-in-memory question.**
4. **The store design,** topic by topic.
5. **Names** you would change, each with your proposal.
6. **Where you disagree with the builders' views** in the README's table, written after all of the above.
7. **What you are unsure of,** and what you couldn't verify.

Write for Magnus: 25 years of .NET, and he hasn't read the spike's code in detail. Lead with conclusions. Use the code's
own names verbatim, explain any term you coin when you first use it, and use no abbreviations.

Report back with the path of your file and its headline, in at most 15 lines.
