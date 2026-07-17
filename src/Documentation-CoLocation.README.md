# Documentation Co-Location Pattern

## Overview
Documentation markdown files and example code are co-located with the production source code they document. This eliminates parallel hierarchies and makes documentation trivially discoverable.

There are two co-located documentation conventions, split by audience:

- **`_docs/`** — public documentation, published as part of the website.
- **`dev_docs/`** — internal documentation supporting development: design narratives, investigation notes, TODO plans. Never published to the website.

Never mix the two: `dev_docs` content must not become part of the website, and website content does not belong in `dev_docs`. (The `Solution.UserDocs` and `Solution.DevDocs` projects in `src/SolutionStructure` surface each set in the solution explorer.)

## File Organization

```
src/Compze.Tessaging/
├── Compze.Tessaging.csproj
├── ...production code...
├── _docs/                          ← Public documentation (published on the website)
│   ├── introduction.md             ← Documentation markdown
│   ├── toc.yml                     ← DocFX table of contents for this folder
│   └── TessageHandling.cs          ← Example code for the docs (compiled ONLY into the Website project)
└── dev_docs/                       ← Internal development documentation (never published)
    └── tevent-delivery-model.md
```

Files in `_docs/` folders:
- Contain markdown documentation and code examples
- Code examples are referenced via DocFX `[!code-csharp[](file.cs#region)]` includes
- Use the SAME namespace as the production code where practical
- Are visible and editable in production projects (appear in Solution Explorer)
- Participate in refactoring (rename a class and it updates in `_docs/` files too)
- Are NOT compiled into production assemblies
- ARE compiled into the Website project — so the compiler catches documentation rot when the API changes

## Technical Implementation

### Part 1: Production projects exclude `_docs` code from compilation

Each documented project excludes its example code from compilation while keeping it visible:

```xml
<!-- Exclude _docs files from compilation but keep them visible. -->
<ItemGroup>
  <Compile Remove="_docs\**\*.cs" />
  <None Include="_docs\**\*.cs" />
</ItemGroup>
```

### Part 2: The Website project compiles every example

`src/Websites/Website/Website.csproj` links in and compiles every co-located example across the framework, so a rename or API change that breaks an example breaks the build instead of silently rotting:

```xml
<Compile Include="..\..\Compze.*\**\_docs\*.cs" LinkBase="CompzeDocsExamples" />
```

The Website project carries flex references (see the FlexRef documentation) to the projects whose types the examples use.

### Part 3: DocFX reads `_docs` through directory junctions

DocFX only processes content located under the folder containing `docfx.json`. `src/Websites/Website/Ensure-CoLocatedDocsJunctions.ps1` therefore creates one git-ignored directory junction per documented project — `Compze\Teventive` → `..\..\Compze.Teventive`, and so on. It runs automatically before every docfx build (npm pre-hooks in `package.json` and `buildAndPublish.ps1`) and is safe to run by hand after giving a new project a `_docs` folder.

`docfx.json` (and `docfx-site-only.json`) pull the co-located docs in through the junctions — and ONLY `_docs`; `dev_docs` never enters the content set:

```json
{
  "files": [ "Compze/**/_docs/**/*.{md,yml}" ],
  "exclude": [ "Compze/**/bin/**", "Compze/**/obj/**" ]
}
```

Site links address the junction paths, e.g. `~/Compze/Teventive/Taggregates/_docs/definition.md`.

## File Template

**Example C# file** (`_docs/MyFeature.cs`):

```csharp
// ReSharper disable All

#pragma warning disable // Documentation example code: deliberately illustrative fragments, not production code.

namespace Compze.Tessaging;

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
   - The compiler fails the build when an example rots, instead of the site silently rendering nothing

## Best Practices

1. **Use `#region` tags** in `_docs/*.cs` files to create named snippets for DocFX includes
2. **Disable warnings** at the file top with `#pragma warning disable` (with its rationale on the same line) since these are deliberately illustrative fragments, not production code
3. **Document the purpose** with XML comments explaining these are documentation examples
4. **Keep examples simple** and focused on illustrating one concept
5. **Use real types** from production code where possible to leverage refactoring
6. **Group related documentation** — put multiple related `.md` and `.cs` files in the same `_docs/` folder
7. **Speak the ubiquitous language** — pages, example files, and regions carry the same names the code does (a page about tevents is `tevent-naming.md`, never `event-naming.md`)

## Verification

To verify the setup works:

```powershell
# Production projects do NOT compile _docs example files; the Website project DOES — and fails on rotted examples:
dotnet build Compze.AllProjects.slnx

# The website builds and every DocFX include and link resolves (0 warnings expected):
cd src/Websites/Website
npm run build-site-only   # or: docfx build docfx-site-only.json
```
