# Structural TypeId — Implementation Plan

> **No backward compatibility constraints.** There are zero deployed applications using this system. No persisted data exists. Any format, ID, or behavior can change freely.

Incremental build. Each phase is testable in isolation before moving to the next.

## Phase 1 — New types, testable in isolation ✅ COMPLETE (111 tests)

All Phase 1 code is additive — nothing existing was changed (except `InternalsVisibleTo` in csproj).

### 1.1 TypeId hierarchy + OpenGenericId ✅ (21 tests)
- `StructuralTypeId` (abstract base) — `StringRepresentation` property, equality by string
- `MappedTypeId` — GUID + string `"GUID, 0"`. Equality by GUID (faster).
- `StableNameTypeId` — unmodified `AssemblyQualifiedName`
- `ConstructedTypeId` — mixed AQN with `GUID, 0` components
- `OpenGenericId` — GUID-backed struct, NOT a `StructuralTypeId`
- Files: `src/.../StructuralTypeId.cs`, `src/.../OpenGenericId.cs`

### 1.2 TypeNameParser ✅ (57 tests)
Parses `AssemblyQualifiedName`-format strings into `ParsedTypeName` tree. Round-trips all formats.

Key design detail: `ArraySuffix` is stored separately from `TypeName` so reconstruction puts `[]` in the correct position (after generic args for `List`1[[...]][]`).

- File: `src/.../Implementation/TypeNameParser.cs`
- Tests: `test/.../Refactoring/Naming/TypeNameParser_specification.cs`

### 1.3 TypeNameMapper ✅ (33 tests)
Transforms `Type` ↔ `StructuralTypeId` using parser + mapping dictionaries. Both directions cached.

Key detail: stable assembly lookup uses `SimpleAssemblyName()` extraction since parsed AQN assembly strings may include `Version=..., Culture=..., PublicKeyToken=...`.

- File: `src/.../Implementation/TypeNameMapper.cs`
- Tests: `test/.../Refactoring/Naming/TypeNameMapper_specification.cs`

### Design doc fix found during implementation
The mapped generic definition format in the design doc was `"GUID, 0[[args]]"` — wrong. Fixed to `"GUID[[args]], 0"` to match standard AQN structure (type arguments come before the comma separator).

## Phase 2 — Registration API

## Phase 2 — Registration API (tested in isolation only — nothing uses it yet)

The types exist and pass their own tests, but nothing in the running system references them.

### 2.1 Mapping declaration ✅ (8 tests)
- `TypeMappingsAttribute`, `ITypeMappingDeclaration`, `ITypeMappingRegistrar`, `TypeMappingRegistrar`
- Files: `src/.../TypeMappingDeclaration.cs` (public API), `src/.../Implementation/TypeMappingRegistrar.cs`

### 2.2 Container registration ✅ (8 tests)
- `TypeNameMapperBuilder` — fluent builder with auto-detection of Microsoft assemblies
- File: `src/.../Implementation/TypeNameMapperBuilder.cs`

**Not yet wired in.** No assembly has `[assembly: TypeMappings(...)]` in production code. `TypeNameMapperBuilder` is only used in test code. These are dead code outside of their own tests.

## Phase 3 — Build standalone system, then swap

### 3.1 Revert grafting ✅
The initial 3.1 approach grafted new methods onto the old `ITypeMapper`/`TypeMapper`. This was reverted to get a clean separation. The old system is untouched and fully operational.

### 3.2 Build new standalone system ✅ (9 tests)
Built a completely independent `IStructuralTypeMapper` + `StructuralTypeMapper` side-by-side with the old system:

- **`IStructuralTypeMapper`** — new public interface with:
  - `GetId(Type) → MappedTypeId` / `GetType(MappedTypeId) → Type` / `TryGetType(...)` — GUID-based leaf type lookup
  - `GetIdForTypesAssignableTo(Type) → IEnumerable<MappedTypeId>` — polymorphic query
  - `AssertMappingsExistFor(IEnumerable<Type>)` — validation
  - `ToPersistedTypeString(Type) → string` / `FromPersistedTypeString(string) → Type` — structural string path
- **`StructuralTypeMapper`** — implementation built from `[TypeMappings]` attributes via `TypeNameMapperBuilder`
  - `BuildFromAssemblies(params Assembly[])` factory method
  - Internally holds `TypeNameMapper` + leaf `MappedTypeId` dictionaries
- **`MappedTypeId` + `StructuralTypeId`** — made public (required for cross-assembly interface)
- Files: `src/.../IStructuralTypeMapper.cs`, `src/.../Implementation/StructuralTypeMapper.cs`
- Tests: `test/.../StructuralTypeMapper_specification.cs` (9 tests)

### 3.3 Migrate assemblies to new registration API ✅
All 13 assemblies now have `[assembly: TypeMappings(...)]` + `ITypeMappingDeclaration` declarations:
- `src/Compze.Abstractions/TypeMappingDeclarations.cs`
- `src/Compze.Core/TypeMappingDeclarations.cs`
- `src/Compze.Tessaging/TypeMappingDeclarations.cs`
- `src/Compze.Tessaging.Teventive.TeventStore/TypeMappingDeclarations.cs`
- `src/Compze.Tessaging.Teventive.TeventStore.Typermedia/TypeMappingDeclarations.cs`
- `src/Compze.Typermedia.Client/TypeMappingDeclarations.cs`
- `test/Compze.Tests.Integration/TypeMappingDeclarations.cs`
- `test/Compze.Tests.Performance.Internals/TypeMappingDeclarations.cs`
- `test/Compze.Tests.Common/TypeMappingDeclarations.cs`
- `test/Compze.Internals.Serialization.Newtonsoft.Specifications/TypeMappingDeclarations.cs`
- `samples/AccountManagement/src/AccountManagement.Domain.Tevents/TypeMappingDeclarations.cs`
- `samples/AccountManagement/src/AccountManagement.Server/TypeMappingDeclarations.cs`
- `samples/AccountManagement/src/AccountManagement.API/TypeMappingDeclarations.cs`

Old `AutoGeneratedCurrentAssemblyTypeMapperForRefactoringSupport` files are still present and used by the old system.

### 3.4 Swap old system for new (not yet started)
Wire `IStructuralTypeMapper` into DI and update all consumers:
1. Register `StructuralTypeMapper` as singleton `IStructuralTypeMapper` in DI
2. Update `RenamingDecorator` to take `IStructuralTypeMapper` instead of `ITypeMapper`
3. Update DocumentDb, TeventStore, Transport, etc. to use `IStructuralTypeMapper.GetId()`/`GetType()` with `MappedTypeId`
4. Update `TaggregateTypeValidator`, handler registries to use `IStructuralTypeMapper.AssertMappingsExistFor`
5. The old `ITypeMapper` becomes unused

### 3.5 Remove old infrastructure (not yet started)
Only possible after 3.4:
- `AutoGeneratedCurrentAssemblyTypeMapperForRefactoringSupport` files (13 files)
- `AssemblyMappingReader`
- `TypeMapperAssemblyScanner`
- `TypeMapperSourceCodeGenerator`
- `MissingMappingReporter` (or rewrite for new system)
- `DeterministicTypeId` / UUID v5
- `ComputedTypeIdType` hierarchy
- `AppDomain.AssemblyLoad` hook, `ReentrancyGuard`
- Static singleton `TypeMapper.Instance`
- Old `ITypeMapper` interface + `TypeMapper` implementation
- Old `TypeId` struct

### 3.6 Full test suite
- ✅ All existing tests pass with 3.1–3.3 changes (2,297 passed, 0 failed)
- Remaining: re-verify after 3.4–3.5
