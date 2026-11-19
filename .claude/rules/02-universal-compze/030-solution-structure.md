# Solution structure

Flat layout — each project has its own top-level directory:

- Library projects: `src/<ProjectName>/<ProjectName>.csproj`
- Test projects: `test/<ProjectName>/<ProjectName>.csproj`
- Multiple `.slnx` files exist for different subsets; `Compze.AllProjects.slnx` is the monolith

## Test project naming

- **`.Specifications`** — BDD-style specification projects (preferred for new projects)
- **`.Tests.`** — older integration/unit test projects

## New packable projects

- **Its `<Version>` is our estimate of how ready and stable it is,** as for any version: "The version number for
  something new should be our estimation of how ready/stable it is" (Magnus, 2026-11-18). Such as `0.1.0-alpha` for a
  package whose API is still being designed. Say what you estimated, and why, when you report the new project.
