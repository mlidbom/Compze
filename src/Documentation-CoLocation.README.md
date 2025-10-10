# Documentation Co-Location Pattern

## Overview
Documentation markdown files and example code are co-located with the production source code they document, following the namespace hierarchy. This eliminates parallel hierarchies and makes documentation trivially discoverable.

## File Organization

```
src/Compze/Tessaging/Abstractions/
├── IServiceBusSession.cs          ← Production code
├── MessageTypes.cs                ← Production code  
└── _docs/                         ← Documentation folder
    ├── basics.md                  ← Documentation markdown
    └── MessageHandling.cs         ← Example code for docs (NOT compiled into production)
```

## Naming Convention

**Documentation Folder:** `_docs/`

Files in `_docs/` folders:
- Contain markdown documentation and code examples
- Code examples are referenced via DocFX `[!code-csharp[](file.cs#region)]` includes
- Use the SAME namespace as the production code in the parent folder
- Are visible and editable in production projects (appear in Solution Explorer)
- Participate in refactoring (rename a class and it updates in `_docs/` files too)
- Are NOT compiled into production assemblies
- ARE compiled into the Website project for DocFX processing

## Technical Implementation

### Part 1: Production Projects (via `Directory.Build.props`)

```xml
<PropertyGroup>
  <EnablePackageValidation>true</EnablePackageValidation>
  <!-- Exclude _docs folders from default compile items -->
  <DefaultItemExcludes>$(DefaultItemExcludes);**\_docs\**</DefaultItemExcludes>
</PropertyGroup>

<ItemGroup>
  <!-- Include _docs content as None items so they're visible in Solution Explorer and participate in refactoring -->
  <None Include="**\_docs\**" />
</ItemGroup>
```

This configuration:
- **Excludes** all files in `_docs/` folders from SDK's automatic compilation via `DefaultItemExcludes`
- **Includes** them as `None` items so they remain visible in Solution Explorer
- Applies to ALL projects in the solution automatically

**Note:** We use `DefaultItemExcludes` instead of `<Compile Remove>` because the SDK's default `**/*.cs` glob happens after `Directory.Build.props` is imported. `DefaultItemExcludes` prevents the SDK from including these files in the first place.

### Part 2: Website Project

```xml
<!-- Include all _docs folder contents from the Compze source tree for compilation in the Website project -->
<ItemGroup>
  <Compile Include="..\..\Compze\**\_docs\*.cs" LinkBase="CompzeDocsExamples" />
</ItemGroup>
```

This configuration:
- **Links** all `.cs` files from `_docs/` folders into the Website project
- **Compiles** them into the Website assembly (required for DocFX code includes)
- Makes them available for DocFX markdown processing

## Benefits

1. **Zero Navigation Overhead**
   - Documentation is exactly where the code is
   - No need to "go find the docs"

2. **Namespace Alignment**  
   - Docs use real production namespaces
   - No artificial `Website.docs.*` namespaces

3. **Automatic Refactoring**
   - Rename a type → it updates in example code automatically
   - Move a file → documentation moves with it

4. **Single Source of Truth**
   - One namespace hierarchy
   - One folder structure  
   - One place to look

5. **Easier Maintenance**
   - Change code and docs in the same commit
   - Same PR review for related changes
   - No sync issues between "code" and "docs" hierarchies

## File Template

**Example C# file** (`_docs/MyFeature.cs`):

```csharp
// ReSharper disable All
#pragma warning disable

namespace Compze.Tessaging.Abstractions;

/// <summary>
/// Example code for documentation purposes.
/// This file is visible in production projects but only compiled into the Website project.
/// </summary>
class MyFeatureExamples
{
   #region basic_example
   void UseTheFeature()
   {
      // Example code here
   }
   #endregion

   #region advanced_example  
   void AdvancedUsage()
   {
      // More complex example
   }
   #endregion
}
```

## Markdown Usage

In your markdown file (e.g., `_docs/basics.md`):

```markdown
# My Feature

Here's how to use it:

[!code-csharp[](MyFeature.cs#basic_example)]

And for advanced usage:

[!code-csharp[](MyFeature.cs#advanced_example)]
```

Note: Since the markdown file is in the same `_docs/` folder as the code file, you can reference it with just the filename.

## DocFX Configuration

Ensure `docfx.json` includes both markdown and source directories:

```json
{
  "metadata": [
    {
      "src": [
        {
          "files": [ "**/*.csproj" ],
          "src": "../../src/Compze"
        }
      ]
    }
  ],
  "build": {
    "content": [
      {
        "files": [ "**/*.md", "**/*.yml" ],
        "src": "../../src/Compze"
      }
    ]
  }
}
```

## Migration from Website/docs

When moving existing documentation from `Website/docs/` to production folders:

1. **Create `_docs/` folder** in the corresponding namespace folder
2. **Move markdown files** into the `_docs/` folder
3. **Move .cs example files** into the `_docs/` folder (remove any `.Docs` suffix)
4. **Update namespaces** in `.cs` files to match production code (not `Website.docs.*`)
5. **Update DocFX table of contents** to reference new locations
6. **Test DocFX build** to ensure code includes still work

Example:
```
FROM: Website/docs/tessaging/basics.md
      Website/docs/tessaging/Basics.cs (namespace Website.docs.tessaging)
  TO: src/Compze/Tessaging/Abstractions/_docs/basics.md
      src/Compze/Tessaging/Abstractions/_docs/Basics.cs (namespace Compze.Tessaging.Abstractions)
```

## Best Practices

1. **Use `#region` tags** in `_docs/*.cs` files to create named snippets for DocFX includes
2. **Disable warnings** at the file top with `#pragma warning disable` since these aren't "real" code
3. **Document the purpose** with XML comments explaining these are documentation examples
4. **Keep examples simple** and focused on illustrating one concept
5. **Use real types** from production code where possible to leverage refactoring
6. **Group related documentation** - put multiple related `.md` and `.cs` files in the same `_docs/` folder

## Verification

To verify the setup works:

```powershell
# Production project should NOT compile _docs files
dotnet build src/Compze/Tessaging/Abstractions/Compze.Tessaging.Abstractions.csproj

# Website project SHOULD compile them
dotnet build src/Websites/Website/Website.csproj -v:n | Select-String "_docs"
```

The Website build output should show `_docs\*.cs` files being compiled into the Website assembly.
