# Compze.Serialization.Newtonsoft

Newtonsoft.Json serialization support for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides JSON serialization using [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json), configured for Compze's needs:

- **Preconfigured serialization settings** — handles non-public members, and type renaming for tevent versioning
- **Tessage serialization** — serializes and deserializes tevents, tommands, tueries and their results
- **The registrars** — `NewtonsoftSerializer()` fills the one serializer parameter an endpoint takes and the pure
  client's; the per-store registrars fill the tevent store's and the document db's

## Installation

```shell
dotnet add package Compze.Serialization.Newtonsoft
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging/) | The endpoints whose serializer parameter this package fills |
| [Compze.Teventive.TeventStore](https://www.nuget.org/packages/Compze.Teventive.TeventStore/) | The tevent store whose serializer this package provides |
| [Compze.DocumentDb](https://www.nuget.org/packages/Compze.DocumentDb/) | The document db whose serializer this package provides |

## License

Apache-2.0
