using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Compze.Contracts;
using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Contracts.Contract;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the distinction between the two non-public namespace kinds of
/// <c>.claude/rules/02-universal-compze/01-strategy/020-highlight-public-vs-internal-parts-of-projects.md</c>:<br/>
/// an Internal namespace holds code deliberately shared with other Compze assemblies through <see cref="InternalsVisibleToAttribute"/>,<br/>
/// while a Private namespace holds code no other assembly may use — whether or not an InternalsVisibleTo grant happens to make it<br/>
/// technically accessible.</summary>
///<remarks>The compiler cannot tell the two kinds apart — an InternalsVisibleTo grant opens every internal type alike — so this policy<br/>
/// reads the compiled assemblies' metadata instead: every type reference each Compze assembly makes is checked against Private<br/>
/// namespaces of the assembly declaring the referenced type. A reference into a foreign Private namespace means one of two things:<br/>
/// the consumer must stop reaching in, or the type is genuinely shared and belongs in an Internal namespace.</remarks>
public static class PrivateNamespaceIsolationPolicy
{
   [XF] public static void No_assembly_references_a_type_in_another_assemblys_Private_namespace()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violations = AllLoadedCompzeAssemblies()
                      .SelectMany(assembly => ForeignPrivateNamespaceTypeReferencesOf(assembly)
                                             .Select(referencedType => $"{assembly.GetName().Name} -> {referencedType}"))
                      .Distinct()
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>The isolation scan above can only see consumers whose assemblies are loaded beside this test project. This guard<br/>
   /// fails when an <see cref="InternalsVisibleToAttribute"/> grant names a repo project that is not loaded — closing the blind<br/>
   /// spot requires adding a project reference to <c>Compze.Tests.CodePolicies</c> so the consumer's references get scanned too.</summary>
   [XF] public static void Every_InternalsVisibleTo_consumer_that_exists_in_the_repo_is_loaded_for_scanning()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();
      var loadedAssemblyNames = AllLoadedCompzeAssemblies().Select(it => it.GetName().Name!).ToHashSet(StringComparer.Ordinal);

      var violations = AllLoadedCompzeAssemblies()
                      .SelectMany(assembly => assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
                                             .Select(grant => grant.AssemblyName.Split(',')[0].Trim())
                                             .Where(consumer => consumer.StartsWithOrdinal("Compze")
                                                             && !loadedAssemblyNames.Contains(consumer)
                                                             && IsRepoProject(consumer))
                                             .Select(consumer => $"{assembly.GetName().Name} grants InternalsVisibleTo {consumer}, which is not loaded for scanning"))
                      .Distinct()
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   static IReadOnlyList<Assembly> AllLoadedCompzeAssemblies() =>
      [
         .. AppDomain.CurrentDomain.GetAssemblies()
                     .Where(assembly => !assembly.IsDynamic && assembly.GetName().Name?.StartsWithOrdinal("Compze") == true)
      ];

   ///<summary>Every type this assembly's compiled metadata references that lives in a Private namespace of a different Compze assembly.</summary>
   static IEnumerable<string> ForeignPrivateNamespaceTypeReferencesOf(Assembly assembly)
   {
      using var peReader = new PEReader(File.OpenRead(assembly.Location));
      var metadata = peReader.GetMetadataReader();
      foreach(var handle in metadata.TypeReferences)
      {
         var typeReference = metadata.GetTypeReference(handle);
         while(typeReference.ResolutionScope.Kind == HandleKind.TypeReference) // a nested type's namespace lives on its outermost declaring type
            typeReference = metadata.GetTypeReference((TypeReferenceHandle)typeReference.ResolutionScope);
         if(typeReference.ResolutionScope.Kind != HandleKind.AssemblyReference) continue;

         var declaringAssembly = metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)typeReference.ResolutionScope).Name);
         if(!declaringAssembly.StartsWithOrdinal("Compze")) continue;

         var @namespace = metadata.GetString(typeReference.Namespace);
         if(@namespace.Split('.').Contains("Private"))
            yield return $"{@namespace}.{metadata.GetString(typeReference.Name)}";
      }
   }

   static bool IsRepoProject(string projectName)
   {
      var repositoryRoot = FindRepositoryRoot();
      return File.Exists(Path.Combine(repositoryRoot, "src", projectName, projectName + ".csproj"))
          || File.Exists(Path.Combine(repositoryRoot, "test", projectName, projectName + ".csproj"));
   }

   static string FindRepositoryRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);
      while(directory is not null && !File.Exists(Path.Combine(directory.FullName, "Compze.AllProjects.slnx")))
         directory = directory.Parent;
      State.Assert(directory is not null, () => $"Found no Compze.AllProjects.slnx above {AppContext.BaseDirectory}.");
      return directory!.FullName;
   }
}
