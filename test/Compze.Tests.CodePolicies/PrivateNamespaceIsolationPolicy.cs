using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the distinction between the two non-public namespace kinds of
/// <c>.claude/rules/02-universal-compze/01-strategy/020-highlight-public-vs-internal-parts-of-projects.md</c>:<br/>
/// an <c>_internal</c> namespace holds code deliberately shared with other Compze assemblies through <see cref="InternalsVisibleToAttribute"/>,<br/>
/// while a <c>_private</c> namespace holds code no other assembly may use — whether or not an InternalsVisibleTo grant happens to make it<br/>
/// technically accessible. Both directions are enforced: <c>_private</c> is never reached into, and <c>_internal</c> never overstates —<br/>
/// a type no other assembly consumes belongs under <c>_private</c>.</summary>
///<remarks>The compiler cannot tell the two kinds apart — an InternalsVisibleTo grant opens every internal type alike — so this policy<br/>
/// reads the compiled assemblies' metadata instead: every type reference each Compze assembly makes is checked against the <c>_private</c><br/>
/// namespaces of the assembly declaring the referenced type, and every type living in an <c>_internal</c> namespace is checked to actually<br/>
/// have such a foreign consumer. Together the two tests make the section names ground truth: a violation either way names the type,<br/>
/// and the fix is moving it to the section that tells the truth.</remarks>
public static class PrivateNamespaceIsolationPolicy
{
   [XF] public static void No_assembly_references_a_type_in_another_assemblys__private_namespace()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violations = AppDomain.CurrentDomain.AllCompzeAssemblies()
                      .SelectMany(assembly => ForeignCompzeTypeReferencesOf(assembly)
                                             .Where(reference => reference.Namespace.Split('.').Contains("_private"))
                                             .Select(reference => $"{assembly.GetName().Name} -> {reference.Namespace}.{reference.Name}"))
                      .Distinct()
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>The converse guarantee: an <c>_internal</c> namespace claims its types are shared through <see cref="InternalsVisibleToAttribute"/>,<br/>
   /// so a type there that no other assembly references overstates — it belongs under <c>_private</c>. Together with the isolation test above<br/>
   /// this keeps the classification self-maintaining: a new foreign consumer of a <c>_private</c> type fails the test above and forces the<br/>
   /// promotion to <c>_internal</c>; the last consumer of an <c>_internal</c> type disappearing fails this test and forces the demotion to <c>_private</c>.</summary>
   [XF] public static void Every_type_in_an__internal_namespace_is_referenced_by_another_assembly()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var foreignReferencedTypes = AppDomain.CurrentDomain.AllCompzeAssemblies()
                                  .SelectMany(ForeignCompzeTypeReferencesOf)
                                  .ToHashSet();

      var violations = AppDomain.CurrentDomain.AllCompzeLibraryTypes()
                      .Where(type => type is { IsNested: false }
                                  && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                  && type.Namespace?.Split('.').Contains("_internal") == true
                                  && !foreignReferencedTypes.Contains((type.Assembly.GetName().Name!, type.Namespace!, type.Name)))
                      .Select(type => type.FullName!)
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>Every type this assembly's compiled metadata references that a different Compze assembly declares: the declaring<br/>
   /// assembly, the namespace, and the type name — nested types resolved to their outermost declaring type, which is what the<br/>
   /// namespace sections classify.</summary>
   static IEnumerable<(string DeclaringAssembly, string Namespace, string Name)> ForeignCompzeTypeReferencesOf(Assembly assembly)
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

         yield return (declaringAssembly, metadata.GetString(typeReference.Namespace), metadata.GetString(typeReference.Name));
      }
   }
}
