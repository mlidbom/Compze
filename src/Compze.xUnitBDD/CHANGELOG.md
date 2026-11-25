# Changelog

All notable changes to Compze.xUnitBDD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.7.0-beta

- **`ISharesOneScenario`: a class's scenario built once for all of its facts.** xUnit makes a new instance of a class for each of its facts, so what the constructors set up is set up again for each. A class that implements `ISharesOneScenario` builds each part of its scenario with `ISharesOneScenario.Once(() => ...)` as its first fact is set up, and the facts after it get the same parts. The scenario ends as xUnit finishes the class, however many of its facts ran: each part that is disposable is disposed, the last built first, and for a part that is a task, built asynchronously, what it completed with, once it has. A part that throws as it is built fails every fact of the class with its exception, and isn't built again. Each class has a scenario of its own, a nested class that derives from one included, and facts that run in parallel wait for the part one of them builds. It is an xUnit class fixture, `ScenarioOfTheClass`. The README has a section on it.
- **Depends on Compze.Contracts.**
- **An `[XF]` declared on an abstract class fails, instead of silently never running:** an `[XF]` runs only in the class that declares it, and xUnit never runs an abstract class, so such a fact ran in no class at all, and nothing said so. Now each class derived from the abstract class lists the fact as a test that fails, saying to make it a `[Fact]`, which runs in every derived class.
  - **What a consumer could notice:** facts that never ran now fail, once in each derived class, until each is made a `[Fact]` or moved to a class that isn't abstract.
- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.xUnit 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-beta

### Changed
- Requires xUnit.net v3 4.0 or later (below 5.0). xUnit.net 4.0 changed the extensibility API in ways that are not
  binary compatible with 3.x, in either direction: 0.5.1-beta fails at runtime under xUnit.net 4.x, and this version
  fails under xUnit.net 3.x. Projects still on xUnit.net 3.x should stay on 0.5.1-beta.

## 0.5.1-beta

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-beta

- Initial release
