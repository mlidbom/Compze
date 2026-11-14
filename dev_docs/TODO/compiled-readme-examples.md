# README examples that compile and run

Decided by Magnus, 2026-11-14 ("Good idea"), not yet started.

## The problem

The packages' READMEs hold C# examples that nothing compiles. Compze.Threading 0.11.0-alpha went out to nuget.org with
examples that didn't compile: one called `IAwaitableThreadShared.WithDefaultTimeouts`, which has never existed, and four
declared `using var lock = ...`, where `lock` is a keyword. JAStudio found the first by trying it. They're fixed,
but nothing stops the next one: a renamed or removed member breaks every README example that uses it, silently.

Fifteen package READMEs hold C# examples, 70 blocks in all (counted 2026-11-14): Compze.Must 17,
Compze.Threading.Testing 15, Compze.Threading 8, Compze.Underscore 6, Compze.Contracts 6, Compze.xUnitMatrix 5,
Compze.Unit 3, Compze.DependencyInjection 3, and one each in Compze.xUnitBDD, Compze.Threading.Interprocess,
Compze.Teventive.TeventStore, Compze.Tessaging, Compze.Tessaging.Hosting.Testing, Compze.Internals.Logging and
Compze.Internals.Logging.Serilog.

## The plan

- **Each example lives as code in a specification project,** so it compiles with the build, and runs with the suite,
  which also shows that it does what the README says it does.
- **A tool copies each example into its README,** between markers that name it. The MarkdownSnippets tool (the
  `mdsnippets` dotnet tool) works that way: a `snippet: <name>` line in the markdown, `begin-snippet: <name>` and
  `end-snippet` comments around the code.
- **A check fails when a README is out of date with its examples,** in the suite (Compze.Tests.CodePolicies, beside the
  other policies) and so in CI.

## To decide when the work starts

- **MarkdownSnippets, or a small tool of our own?** MarkdownSnippets is mature and does exactly this. One of our own
  avoids a dependency, and can be a DevScripts command.
- **Examples that aren't real code now** have to become real code, such as those that elide a class's body with `/**/`
  or use an undefined `ready`. Some may read worse as complete code; decide one by one.
- **The website** (`src/Websites/Website/`): whether its pages' examples come from the same source.
