using System.IO.Compression;
using System.Xml.Linq;
using Mono.Cecil;

namespace PostReleaseBindingCheck.Packages;

/// <summary>
/// One Compze package of the <see cref="PostReleasePackageSet"/>: its id and version, its <see cref="PackageOrigin"/>,<br/>
/// and the assemblies a consumer's application loads from it.
/// </summary>
public sealed class Package : IDisposable
{
   const string LibraryFolder = "lib/";

   readonly IReadOnlyList<MemoryStream> _assemblyImages;

   Package(string id, string version, PackageOrigin origin, IReadOnlyList<AssemblyDefinition> assemblies, IReadOnlyList<MemoryStream> assemblyImages)
   {
      Id = id;
      Version = version;
      Origin = origin;
      Assemblies = assemblies;
      _assemblyImages = assemblyImages;
   }

   /// <summary>The package id, such as <c>Compze.Internals.Logging</c>.</summary>
   public string Id { get; }

   /// <summary>The package version, such as <c>0.5.0-alpha</c>.</summary>
   public string Version { get; }

   public PackageOrigin Origin { get; }

   /// <summary>
   /// The assemblies in the package's single <c>lib/&lt;target framework&gt;</c> folder: the ones a consumer's application loads.
   /// </summary>
   internal IReadOnlyList<AssemblyDefinition> Assemblies { get; }

   /// <summary><c>Compze.Internals.Logging 0.5.0-alpha (this release)</c>, or <c>... (on nuget.org)</c>.</summary>
   public override string ToString() => $"{Id} {Version} ({OriginDescription})";

   string OriginDescription => Origin switch
   {
      PackageOrigin.ThisRelease => "this release",
      PackageOrigin.NuGetOrg => "on nuget.org",
      _ => throw new InvalidOperationException($"Unknown {nameof(PackageOrigin)}: {Origin}")
   };

   /// <summary>
   /// Reads the package's id and version from the nuspec in <paramref name="nupkgPath"/>, and its assemblies from its <c>lib</c> folder,<br/>
   /// with <paramref name="assemblyResolver"/> resolving the references they make to other assemblies.
   /// </summary>
   internal static Package ReadNupkg(string nupkgPath, PackageOrigin origin, IAssemblyResolver assemblyResolver)
   {
      using var nupkg = ZipFile.OpenRead(nupkgPath);
      var metadata = ManifestMetadata(nupkg, nupkgPath);
      var id = RequiredElementValue(metadata, "id", nupkgPath);
      var version = RequiredElementValue(metadata, "version", nupkgPath);

      var assemblyEntries = AssemblyEntriesOfTheOnlyTargetFramework(nupkg, $"{id} {version}");
      var assemblyImages = assemblyEntries.Select(ReadIntoMemory).ToList();
      var assemblies = assemblyImages.Select(image => AssemblyDefinition.ReadAssembly(image, new ReaderParameters { AssemblyResolver = assemblyResolver })).ToList();
      return new Package(id, version, origin, assemblies, assemblyImages);
   }

   public void Dispose()
   {
      foreach(var assembly in Assemblies) assembly.Dispose();
      foreach(var image in _assemblyImages) image.Dispose();
   }

   static XElement ManifestMetadata(ZipArchive nupkg, string nupkgPath)
   {
      var manifestEntry = nupkg.Entries.Single(it => !it.FullName.Contains('/', StringComparison.Ordinal) && it.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
      using var manifestStream = manifestEntry.Open();
      var manifest = XDocument.Load(manifestStream);
      var metadata = manifest.Root?.Elements().SingleOrDefault(it => it.Name.LocalName == "metadata");
      State.Assert(metadata != null, () => $"The nuspec in {nupkgPath} has no metadata element.");
      return metadata;
   }

   static string RequiredElementValue(XElement metadata, string elementName, string nupkgPath)
   {
      var element = metadata.Elements().SingleOrDefault(it => it.Name.LocalName == elementName);
      State.Assert(element != null, () => $"The nuspec in {nupkgPath} has no {elementName}.");
      return element.Value.Trim();
   }

   /// <summary>
   /// The <c>lib/&lt;target framework&gt;/*.dll</c> entries. Every Compze package targets one framework; a package that targets<br/>
   /// several would need the check to pick the folder a consumer's application loads, which it does not know how to do.
   /// </summary>
   static IReadOnlyList<ZipArchiveEntry> AssemblyEntriesOfTheOnlyTargetFramework(ZipArchive nupkg, string package)
   {
      var assemblyEntries = nupkg.Entries.Where(IsLibraryAssembly).ToList();
      var targetFrameworks = assemblyEntries.Select(TargetFrameworkFolder).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
      State.Assert(targetFrameworks.Count <= 1,
                   () => $"{package} has assemblies for {string.Join(", ", targetFrameworks)}. The post-release binding check reads a package's only target framework, and must learn which one a consumer loads before it can check a package with several.");
      return assemblyEntries;

      static bool IsLibraryAssembly(ZipArchiveEntry entry) =>
         entry.FullName.StartsWith(LibraryFolder, StringComparison.OrdinalIgnoreCase)
      && entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
      && entry.FullName.Count(character => character == '/') == 2;

      static string TargetFrameworkFolder(ZipArchiveEntry entry) => entry.FullName.Split('/')[1];
   }

   /// <summary>Cecil reads an assembly from a seekable stream, which a zip entry's is not.</summary>
   static MemoryStream ReadIntoMemory(ZipArchiveEntry entry)
   {
      var image = new MemoryStream();
      using(var entryStream = entry.Open()) entryStream.CopyTo(image);
      image.Position = 0;
      return image;
   }
}
