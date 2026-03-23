# TypeIdentifier Design

> **No backward compatibility constraints.** There are zero deployed applications using this system. No persisted data exists. Any format, ID, or behavior can change freely.

> **Draft.** Direction, not specification. The goal is a simpler, more transparent system — adjust as we go.


---

## Two tiers of type handling

1. **Stable assemblies** — types whose names won't change. Their full `TypeName, Assembly` pairs pass through into the persisted form untouched.
   - .NET runtime assemblies are stable by default (detected automatically via public key token or well-known names)
   - Users can declare additional assemblies stable:
     ```csharp
     mapper.UseStableNameStrategyForAssembliesContaining<NodaTime.Instant, SomeOtherLib.Foo>();
     ```

2. **Mapped assemblies** — types that need rename-safety. Leaf types and Open generic definitions require GUID assignments:
   ```csharp
   mapper.MapTypesFromAssemblyContaining<MyEntity>();
   ```

---

## Assembly-level mapping declaration

Each assembly declares its mappings via an assembly-level attribute pointing out the class that does the actual mapping. Registration is fluent:

The framework enforces that a mapping class only maps types from its own assembly. Mapping a type from another assembly is an error at registration time.

---

## Per-container registration (no static global mapping from TypeIdentifier to Type)

```csharp
   mapper
      .MapTypesFromAssemblyContaining<MyEntity>()
      .MapTypesFromAssemblyContaining<AnotherDomainType>()
      .UseStableNameStrategyForAssembliesContaining<SomeLibraryType>();
```

---- **Stable assembly detection**: At **setup time**, assemblies are checked by public key token to determine stability. At **parse time**, the `$type` string only contains assembly names (no tokens), so stable assembly lookup is by name against the pre-built set. Microsoft uses a small, known set of public key tokens.  These are hardcoded as stable by default. Users can also register additional stable assemblies by public key token:
  ```csharp
  mapper.UseStableNameStrategyForPublicKeyToken("xxxxxxxxxxxx"); // all assemblies signed with this token
  mapper.UseStableNameStrategyForAssembliesContaining<NodaTime.Instant>(); // or by marker type
  ```
