# Compze.Serialization.Newtonsoft

Newtonsoft.Json serialization support for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides JSON serialization using [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json), configured for Compze's needs:

- **Preconfigured serialization settings** — Handles non-public members and type renaming for event versioning
- **Event serialization** — Serializes and deserializes Compze events, commands, and query results
- **Pluggable serializer** — Integrates with Compze's pluggable component architecture

## Installation

```shell
dotnet add package Compze.Serialization.Newtonsoft
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Core](https://www.nuget.org/packages/Compze.Core) | Core abstractions |
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |

## License

Apache-2.0
