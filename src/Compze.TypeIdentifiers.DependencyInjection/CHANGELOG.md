# Changelog

All notable changes to Compze.TypeIdentifiers.DependencyInjection will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.TypeIdentifiers 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.1.0-alpha

- Initial pre-release. The package exists because composing a type map is a container concern and identifying types is not: `Compze.TypeIdentifiers` stays dependency-free, and the composition moves here.
- `RequireMappedTypesFromAssemblyContaining<T>()` and `RequireStableTypeNamesFromAssemblyContaining<T>()` on `IComponentRegistrar`: a component declares the assemblies whose type identity it depends on, the way it declares any other dependency. Declaring the same assembly from several components costs nothing; declaring it two different ways — mapped by one, stable-named by another — throws when the map is built, because the two disagree about what that assembly's persisted type identity is.
- `RegisterTypeMap()` installs the container's one `ITypeMap` on whichever requirement is declared first. What is registered is a recipe, not a map: it reads the whole finished requirement set when the container builds, so every declaration is included no matter what order they arrived in. A container that already carries an `ITypeMap` keeps it.
