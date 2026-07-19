# TypeIdentifier Design

> **No backward compatibility constraints.** There are zero deployed applications using this system. No persisted data exists. Any format, ID, or behavior can change freely.

> **Draft.** Direction, not specification. The goal is a simpler, more transparent system — adjust as we go.


---

## Two tiers of type handling

1. **Stable assemblies** — types whose names won't change. Their full `TypeName, Assembly` pairs pass through into the persisted form untouched.
   - .NET runtime assemblies are stable by default (detected automatically via public key token or well-known names)
   - Users can declare additional assemblies stable:
     ```csharp
     builder.UseStableNameStrategyForAssemblyContaining<NodaTime.Instant>();
     builder.UseStableNameStrategyForAssemblyContaining<SomeOtherLib.Foo>();
     ```

2. **Mapped assemblies** — types that need rename-safety. Leaf types and Open generic definitions require GUID assignments:
   ```csharp
   builder.MapTypesFromAssemblyContaining<MyEntity>();
   ```

---

## Assembly-level mapping declaration

Each assembly declares its mappings via an assembly-level attribute pointing out the class that does the actual mapping. Registration is fluent:

The framework enforces that a mapping class only maps types from its own assembly. Mapping a type from another assembly is an error at registration time.

---

## Per-container declaration (no static global mapping from TypeIdentifier to Type)

An `ITypeMap` is built once, in its entirety, by a `TypeMapBuilder`, and is immutable thereafter — there is no
registering into a live map:

```csharp
   ITypeMap typeMap = new TypeMapBuilder().MapTypesFromAssemblyContaining<MyEntity>()
                                          .MapTypesFromAssemblyContaining<AnotherDomainType>()
                                          .UseStableNameStrategyForAssemblyContaining<SomeLibraryType>()
                                          .Build();
```

Applications rarely build one by hand. With `Compze.TypeIdentifiers.DependencyInjection`, each component instead
declares the assemblies whose type identity *it* needs, where it is registered, and the container composes one map
covering every declaration:

```csharp
   registrar.RequireMappedTypesFromAssemblyContaining<MyEntity>();
   registrar.RequireStableTypeNamesFromAssemblyContaining<SomeLibraryType>();
```

Several components requiring the same assembly is ordinary and costs nothing. Two components requiring one assembly
*two different ways* is a disagreement about that assembly's persisted type identity, and throws when the map is built.

---- **Stable assembly detection**: At **setup time**, assemblies are checked by public key token to determine stability. At **parse time**, the `$type` string only contains assembly names (no tokens), so stable assembly lookup is by name against the pre-built set. Microsoft uses a small, known set of public key tokens.  These are hardcoded as stable by default. Users can also register additional stable assemblies by public key token:
  ```csharp
  mapper.UseStableNameStrategyForPublicKeyToken("xxxxxxxxxxxx"); // all assemblies signed with this token
  mapper.UseStableNameStrategyForAssemblyContaining<NodaTime.Instant>(); // or by marker type
  ```
