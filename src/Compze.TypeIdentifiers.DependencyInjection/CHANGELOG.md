# Changelog

All notable changes to Compze.TypeIdentifiers.DependencyInjection will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- Initial pre-release. The package exists because composing a type map is a container concern and identifying types is not: `Compze.TypeIdentifiers` stays dependency-free, and the composition moves here.
- `RequireMappedTypesFromAssemblyContaining<T>()` and `RequireStableTypeNamesFromAssemblyContaining<T>()` on `IComponentRegistrar`: a component declares the assemblies whose type identity it depends on, the way it declares any other dependency. Declaring the same assembly from several components costs nothing; declaring it two different ways — mapped by one, stable-named by another — throws when the map is built, because the two disagree about what that assembly's persisted type identity is.
- `RegisterTypeMap()` installs the container's one `ITypeMap` on whichever requirement is declared first. What is registered is a recipe, not a map: it reads the whole finished requirement set when the container builds, so every declaration is included no matter what order they arrived in. A container that already carries an `ITypeMap` keeps it.
