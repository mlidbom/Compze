using Compze.Internals.SystemCE;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces that Tessaging's two siblings stay blind to each other: no source file under a TessageBus
/// folder imports a Typermedia namespace, and vice versa. Only the shared substrate and the endpoint
/// composition layer may see both — the seam a future project split would cut along.</summary>
public static class TessagingSiblingIndependencePolicy
{
   [XF] public static void TessageBus_sources_never_import_Typermedia_namespaces() =>
      UsingDirectiveViolations(siblingFolder: "TessageBus", forbiddenNamespacePrefix: "Compze.Tessaging.Typermedia")
        .Must().SequenceEqual(Array.Empty<string>());

   [XF] public static void Typermedia_sources_never_import_TessageBus_namespaces() =>
      UsingDirectiveViolations(siblingFolder: "Typermedia", forbiddenNamespacePrefix: "Compze.Tessaging.TessageBus")
        .Must().SequenceEqual(Array.Empty<string>());

   static List<string> UsingDirectiveViolations(string siblingFolder, string forbiddenNamespacePrefix)
   {
      var repositoryRoot = CompzeRepository.SourceFolder();
      var siblingSourceFolders = new[]
      {
         CompzeRepository.SourceFolder("src", "Compze.Tessaging", siblingFolder),
         CompzeRepository.SourceFolder("src", "Compze.Tessaging.Abstractions", siblingFolder)
      };

      return siblingSourceFolders
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .SelectMany(file => File.ReadLines(file)
                                    .Select((line, index) => (line, lineNumber: index + 1))
                                    .Where(it => it.line.TrimStart().StartsWithOrdinal("using ")
                                              && it.line.ContainsOrdinal(forbiddenNamespacePrefix))
                                    .Select(it => $"{Path.GetRelativePath(repositoryRoot, file)}({it.lineNumber}): {it.line.Trim()}"))
            .Order(StringComparer.Ordinal)
            .ToList();
   }
}
