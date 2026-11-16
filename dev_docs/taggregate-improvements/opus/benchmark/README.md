# The benchmark: the bytes a taggregate instance retains

Written by taggregates-opus, in JAStudio, on 2026-11-16. It measured the numbers in
[the per-class routing](../2-tevent-routing-decided-once-per-class.md) against Compze.Teventive 0.6.0-alpha.

`generate.py` writes a `Program.cs` for a note-like taggregate. It has a given number of components, each with a given
number of string properties and one applier per property. The program then measures the bytes retained after full
collections, for 10,000 instances, of:

- plain objects of the same shape;
- the taggregate restored through `LoadFromHistory` from one creation tevent;
- the same taggregates after one publish on each component, committed.

## Running it

Run it in a folder outside this repository: inside it, the root's `Directory.Build.props` would apply. The folder
needs `generate.py` and this project file, `TaggregateMemory.csproj`. It's kept here as text, because Compze's
DevScripts treat every `.csproj` in the repository as a project.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <ServerGarbageCollection>false</ServerGarbageCollection>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Compze.Teventive" Version="0.6.0-alpha" />
  </ItemGroup>
</Project>
```

Then, for 15 components of 3 properties:

```
python generate.py 15 3 Program.cs
dotnet run -c Release
```

The generated program restores its taggregates through the `[Obsolete]` `SetTaggregateVersionInternal`. Once
`LoadFromSnapshot` exists, it should use that instead.

## Until the redesign has a specification of its own

The per-class routing adds a specification to Compze of the bytes a taggregate retains per instance, with a bound.
After that, a regression shows in Compze's own test run, and this benchmark is needed only to compare with 0.6.0-alpha.
