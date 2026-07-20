# Solution structure

Flat layout — each project has its own top-level directory:

- Library projects: `src/<ProjectName>/<ProjectName>.csproj`
- Test projects: `test/<ProjectName>/<ProjectName>.csproj`
- Multiple `.slnx` files exist for different subsets; `Compze.AllProjects.slnx` is the monolith

## Test project naming

- **`.Specifications`** — BDD-style specification projects (preferred for new projects)
- **`.Tests.`** — older integration/unit test projects

## New packable projects

- Set `<Version>` to an early pre-release version (e.g., `0.1.0-alpha.1`). **NEVER** use `1.0.0` or any
  stable-looking version for something new and in development.
