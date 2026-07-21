using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Guards the code policies' blind spots: every policy in this project inspects the Compze assemblies loaded beside the
/// test assembly, and an assembly that is not loaded is silently exempt from every policy. These guards make the loaded set
/// provably cover the repo.</summary>
///<remarks>The two guards compose inductively: every src project's assembly must be loaded, and every
/// <see cref="InternalsVisibleToAttribute"/> grant made by a loaded assembly must have its consumer loaded — so all shipped
/// code is scanned, and so is every assembly that can even reach another assembly's internals. Test projects without a grant
/// cannot touch internals at all, so they need no blanket loading. A failure's fix is always the same: add the named project
/// as a project reference of <c>Compze.Tests.CodePolicies</c>.</remarks>
public static class AssemblyScanCoveragePolicy
{
   [XF] public static void Every_src_projects_assembly_is_loaded_for_scanning()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();
      var loadedAssemblyNames = AppDomain.CurrentDomain.AllCompzeAssemblies().Select(it => it.GetName().Name!).ToHashSet(StringComparer.Ordinal);

      var violations = Directory.GetDirectories(Path.Combine(CompzeRepository.Root, "src"))
                                .Select(directory => Path.GetFileName(directory)!)
                                .Where(projectName => File.Exists(Path.Combine(CompzeRepository.Root, "src", projectName, projectName + ".csproj"))
                                                   && !loadedAssemblyNames.Contains(projectName))
                                .Select(projectName => $"{projectName} is not loaded for scanning")
                                .Order(StringComparer.Ordinal)
                                .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>The policies that inspect InternalsVisibleTo consumers — the Private-isolation scan above all — can only see
   /// consumers whose assemblies are loaded. This guard fails when an <see cref="InternalsVisibleToAttribute"/> grant names a
   /// repo project that is not loaded, closing the blind spot the grant would otherwise open.</summary>
   [XF] public static void Every_InternalsVisibleTo_consumer_that_exists_in_the_repo_is_loaded_for_scanning()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();
      var loadedAssemblyNames = AppDomain.CurrentDomain.AllCompzeAssemblies().Select(it => it.GetName().Name!).ToHashSet(StringComparer.Ordinal);

      var violations = AppDomain.CurrentDomain.AllCompzeAssemblies()
                      .SelectMany(assembly => assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
                                             .Select(grant => grant.AssemblyName.Split(',')[0].Trim())
                                             .Where(consumer => consumer.StartsWithOrdinal("Compze")
                                                             && !loadedAssemblyNames.Contains(consumer)
                                                             && CompzeRepository.IsRepoProject(consumer))
                                             .Select(consumer => $"{assembly.GetName().Name} grants InternalsVisibleTo {consumer}, which is not loaded for scanning"))
                      .Distinct()
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }
}
