# Correcting Issues at Reported Lines

## Core Approach

For each reported issue: **read the file at the reported line, understand the context, then edit.**

Do NOT blindly apply changes at line numbers. The report reflects files as they were when the inspection ran. As you edit files, line numbers shift. Always read the file to find the actual code.

## Batch Editing Strategy

1. **Group issues by file.** Read each file once, apply all fixes for that file, move on.
2. **Use multi-edit operations** to apply multiple fixes within one file or across files in a single call.
3. **Build after each batch** of files (every ~10-20 files). Catching errors early avoids cascading confusion.
4. **Run the full test suite once** at the end after the build is clean.

## Handling Specific Inspection Types

### MemberCanBeInternal

Change `public` to `internal` on the reported member. Watch for:

- **Override methods**: If you make an `abstract` or `virtual` base method `internal`, **all overrides must also become `internal`**. The compiler enforces this (`CS0507`), so the build will catch it.
- **Serialized properties**: Properties serialized by Newtonsoft (or similar) must have a `public` getter. Newtonsoft only serializes public properties by default. Making the getter `internal` causes silent data loss — the property deserializes as `null`/default. **The compiler will NOT catch this.** Only tests will. Note: private *setters* are fine — Newtonsoft uses reflection to write values regardless of setter accessibility.
- **Solution completeness**: ReSharper analyzes at solution scope. Its analysis is only correct if the solution contains **all** projects that consume the code. Before running inspections, verify that no projects are missing from the solution file.

When a member must stay `public` despite ReSharper's suggestion, add a suppression comment:
```csharp
// ReSharper disable once MemberCanBeInternal — Serialized via Newtonsoft
public TaggregateId TaggregateId { get; set; }
```

### MemberCanBePrivate, ClassCanBeSealed, etc.

Same general approach: read, understand context, apply. The pitfalls above (serialization, cross-assembly, override chains) apply to all visibility/accessibility inspections.

### MemberCanBePrivate — Overloaded Methods

When ReSharper reports that one overload can be private, verify which overload it means. A common pattern is a `public` convenience overload that delegates to a `private` implementation overload. Making the wrong one private breaks external callers. Always read the file to confirm which overload is the entry point before changing visibility.

## Verification

1. **Build**: Catches access modifier conflicts (CS0507, CS0117, CS1929, etc.)
2. **Test**: Catches serialization and runtime issues the compiler can't see
3. Both are mandatory. A clean build is necessary but not sufficient.
