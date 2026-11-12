using System.IO.Compression;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PostReleaseBindingCheck.Specifications.Fixtures;

/// <summary>
/// Makes the nupkgs a specification checks: compiles C# source into an assembly named after the package, and packs it with<br/>
/// a nuspec into <c>lib/net10.0</c>, as <c>C-Pack</c> packs a Compze package. The nupkgs live in a temporary folder that<br/>
/// <see cref="Dispose"/> deletes.
/// </summary>
public sealed class FixturePackages : IDisposable
{
   const string TargetFramework = "net10.0";

   static readonly Lazy<IReadOnlyList<MetadataReference>> DotNetLibraries = new(ReferencesToTheDotNetLibraries);

   readonly string _directory = Path.Combine(Path.GetTempPath(), $"post-release-binding-check-specifications-{Guid.NewGuid()}");

   public FixturePackages() => Directory.CreateDirectory(_directory);

   /// <summary>Packs version <paramref name="version"/> of package <paramref name="id"/>, compiled from <paramref name="source"/> against the packages <paramref name="compiledAgainst"/>.</summary>
   public FixturePackage Pack(string id, string version, string source, params FixturePackage[] compiledAgainst)
   {
      var assemblyImage = Compile(id, source, compiledAgainst);
      var nupkgPath = Path.Combine(_directory, $"{id}.{version}.nupkg");
      using(var nupkg = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
      {
         Write(nupkg, $"{id}.nuspec", Manifest(id, version));
         Write(nupkg, $"lib/{TargetFramework}/{id}.dll", assemblyImage);
      }

      return new FixturePackage(nupkgPath, MetadataReference.CreateFromImage(assemblyImage));
   }

   public void Dispose() => Directory.Delete(_directory, recursive: true);

   static byte[] Compile(string assemblyName, string source, IEnumerable<FixturePackage> compiledAgainst)
   {
      var compilation = CSharpCompilation.Create(assemblyName,
                                                 [CSharpSyntaxTree.ParseText(source)],
                                                 [..DotNetLibraries.Value, ..compiledAgainst.Select(package => package.Assembly)],
                                                 new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
      using var image = new MemoryStream();
      var result = compilation.Emit(image);
      var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
      return errors.Count == 0
                ? image.ToArray()
                : throw new InvalidOperationException($"The fixture source of {assemblyName} does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
   }

   static byte[] Manifest(string id, string version) =>
      System.Text.Encoding.UTF8.GetBytes($"""
                                          <?xml version="1.0" encoding="utf-8"?>
                                          <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                                            <metadata>
                                              <id>{id}</id>
                                              <version>{version}</version>
                                            </metadata>
                                          </package>
                                          """);

   static void Write(ZipArchive nupkg, string entryName, byte[] content)
   {
      using var entry = nupkg.CreateEntry(entryName).Open();
      entry.Write(content);
   }

   /// <summary>The .NET libraries this process runs on: the managed assemblies the runtime trusts, from its own directory.</summary>
   static IReadOnlyList<MetadataReference> ReferencesToTheDotNetLibraries()
   {
      var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
      var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
      return [..trustedAssemblies.Where(path => string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.OrdinalIgnoreCase))
                                 .Select(path => MetadataReference.CreateFromFile(path))];
   }
}
