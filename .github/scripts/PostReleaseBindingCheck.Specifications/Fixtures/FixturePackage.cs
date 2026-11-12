using Microsoft.CodeAnalysis;

namespace PostReleaseBindingCheck.Specifications.Fixtures;

/// <summary>A package a specification made with <see cref="FixturePackages.Pack"/>: its nupkg file, and its assembly to compile other packages against.</summary>
public sealed class FixturePackage
{
   public FixturePackage(string nupkgPath, MetadataReference assembly)
   {
      NupkgPath = nupkgPath;
      Assembly = assembly;
   }

   public string NupkgPath { get; }

   public MetadataReference Assembly { get; }
}
