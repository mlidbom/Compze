# Compze.TypeIdentifiers.DependencyInjection

Dependency-injection composition for [Compze.TypeIdentifiers](https://www.nuget.org/packages/Compze.TypeIdentifiers/): each component declares which assemblies' type mappings it requires, and the container composes one immutable `ITypeMap` from every declaration.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **`RequireMappedTypesFromAssemblyContaining<T>()`** — a component states that it persists or transmits types from `T`'s assembly, and that those types must therefore be mapped to the GUIDs the assembly declares.
- **`RequireStableTypeNamesFromAssemblyContaining<T>()`** — the same declaration for an assembly whose type names are persisted unchanged instead of being replaced by GUIDs.
- **`RegisterTypeMap()`** — installs the container's one `ITypeMap`, composed from the whole finished requirement set when the container is built, so declaration order never decides what a type is persisted as.

"Which assemblies must be mapped" is a dependency like any other, and belongs to the component that has it. Threading a mutable mapper through composition roots put the knowledge in the wrong place: every composition root had to know, and repeat, what its components needed.

Keeping this in its own package leaves the type-identity core dependency-free — a domain's contracts assembly can identify its types without taking a dependency on a container.

## Installation

```shell
dotnet add package Compze.TypeIdentifiers.DependencyInjection
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.TypeIdentifiers](https://www.nuget.org/packages/Compze.TypeIdentifiers/) | The dependency-free type-identity core these declarations compose |
| [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) | The container the declarations are made against |

## License

Apache-2.0
