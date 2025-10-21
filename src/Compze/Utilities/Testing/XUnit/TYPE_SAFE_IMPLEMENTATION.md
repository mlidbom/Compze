# Type-Safe Pluggable Components Theory Attribute - Implementation Summary

## What Was Built

A type-safe refactoring of the `PluggableComponentsTheoryAttribute` system that uses enums instead of strings for component specifications.

## Architecture

### 1. **Enum Definitions** (`TestComponentEnums.cs`)
```csharp
public enum Type1Component
{
   Type1Component1,
   Type1Component2,
   Type1Component3
}

public enum Type2Component
{
   Type2Component1,
   Type2Component2,
   Type2Component3
}
```

### 2. **Helper Class** (`ComponentSkipSpecification.cs`)
Converts enum values to the internal `"ComponentName::Reason"` format:
```csharp
ComponentSkipSpecification.Skip(Type1Component.Type1Component1, "TODO")
// Returns: "Type1Component1::TODO"
```

### 3. **Generic Base Class** (`TypedPluggableComponentsTheoryAttribute<T1, T2>`)
- Inherits from `PluggableComponentsTheoryAttribute`
- Takes two enum types as generic parameters
- Accepts component arrays and reason arrays in constructor
- Converts enums to strings internally for existing infrastructure

### 4. **Concrete Implementation** (`TypedPCTAttribute`)
- Sealed class for actual usage in tests
- Pre-configured with `Type1Component` and `Type2Component`
- Easy to use with named parameters

## Usage Examples

### Simple Skip
```csharp
[TypedPCT(
   skippedComponents: [Type1Component.Type1Component1],
   skipReasons: ["TODO"])]
public void MyTest() { }
```

### Multiple Skips Across Dimensions
Mix and match components from any dimension in a single array - much cleaner!
```csharp
[TypedPCT(
   skippedComponents: [
      Type1Component.Type1Component1, 
      Type1Component.Type1Component3,
      Type2Component.Type2Component3
   ],
   skipReasons: [
      "Not implemented yet", 
      "Deprecated",
      "Unsupported configuration"
   ])]
public void MyTest() { }
```

## Benefits

✅ **Type Safety** - No string typos, enums are validated at compile time
✅ **IntelliSense** - Autocomplete shows all available components
✅ **Refactoring** - Renaming enums updates all usages
✅ **Backward Compatible** - Original string-based `PCT` attribute still works
✅ **No Breaking Changes** - Existing tests unchanged

## Design Considerations

### Why Constructor Parameters Instead of Properties?

C# attributes have strict limitations:
- Properties must be simple types or arrays of simple types
- Complex initialization logic in `init` accessors doesn't work
- Tuples and complex types aren't allowed

Therefore, we use:
- `object[]` arrays that contain enum values (validated at runtime)
- Separate `string[]` arrays for reasons
- Constructor parameters with named parameter syntax

### Why Not Use Tuple Arrays?

This would be ideal but doesn't work with attributes:
```csharp
// ❌ This doesn't work in C# attributes
[TypedPCT(Skipped = [(Type1Component.Type1Component1, "TODO")])]
```

Instead we use parallel arrays:
```csharp
// ✅ This works - and you can mix components from any dimension!
[TypedPCT(
   skippedComponents: [Type1Component.Type1Component1, Type2Component.Type2Component3],
   skipReasons: ["TODO", "Unsupported"])]
```

### Why Single Arrays Instead of Per-Dimension Arrays?

Since we're already using untyped `object[]` for attribute compatibility, there's no benefit to having separate arrays per dimension. A single pair of arrays is much cleaner and allows mixing components from different dimensions naturally.

## Testing

All tests pass successfully:
- ✅ Single component skips work
- ✅ Multiple component skips work
- ✅ Nested scenarios work
- ✅ Skip reasons are displayed correctly
- ✅ Enum-to-string conversion works properly

## Future Enhancements

### Option 1: Support More Dimensions ✅ DONE
Already created base classes for 2, 3, and 4 dimensions:
- `TypedPluggableComponentsTheoryAttribute<T1, T2>`
- `TypedPluggableComponentsTheoryAttribute<T1, T2, T3>`
- `TypedPluggableComponentsTheoryAttribute<T1, T2, T3, T4>`

Can easily extend to more if needed.

### Option 2: Attribute Metadata
Add custom attributes to enum values for better documentation:
```csharp
public enum Type1Component
{
   [Description("Legacy implementation")]
   Type1Component1,
}
```

### Option 3: Builder Pattern
Create a fluent API for more complex scenarios:
```csharp
[PCT]
public void MyTest()
{
   SkipIf<Type1Component>(Type1Component.Component1, "reason");
}
```

### Option 4: Generic Count
Use C# 11+ generic math to support arbitrary number of dimensions:
```csharp
public class TypedPCT<T1, T2, T3, ...> where ... : Enum
```

## Migration Path

1. ✅ **Phase 1** (Complete): Create type-safe infrastructure
2. **Phase 2**: Gradually migrate existing tests to use `TypedPCT`
3. **Phase 3**: Mark string-based `PCT` as obsolete
4. **Phase 4**: Remove string-based API (breaking change)

## Files Created

- `ComponentPermutations/ComponentSkipSpecification.cs` - Helper for enum-to-string conversion
- `ComponentPermutations/TypedPluggableComponentsTheoryAttribute.cs` - Generic base class
- `Tests/ComponentPermutations/TestComponentEnums.cs` - Enum definitions
- `Tests/ComponentPermutations/TypedPCTAttribute.cs` - Concrete typed attribute
- `Tests/ComponentPermutations/WhenAComponentIsMarkedAsExcludedTypeSafe.cs` - Demo tests
