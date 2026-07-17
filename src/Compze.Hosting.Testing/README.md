# Compze.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) endpoint hosting — the testing host that knows nothing of Tessaging, Typermedia, or any other capability.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

The pluggable-component wiring that builds test containers from the current test configuration.

- **`TestingComponentRegistrar`** — the component registrar all test containers are built with; routes connection-string lookups through the test database pool.
- **Pluggable-component wiring** — DI container, serializer, and database pool selection driven by the current test's `PluggableComponents` configuration.

The testing endpoint host itself lives in `Compze.Tessaging.Hosting.Testing`: it registers the concrete
endpoint types with per-tier test wiring, which requires knowing the tiers.

## Installation

```shell
dotnet add package Compze.Hosting.Testing
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Hosting](https://www.nuget.org/packages/Compze.Hosting) | The endpoint hosting mechanism this package tests against |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | The testing features for both of the paradigm's siblings |

## License

Apache-2.0
