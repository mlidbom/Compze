using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the white-box-isolation half of the black-box-testing strategy
/// (<c>.claude/rules/02-universal-compze/01-strategy/005-only-black-box-tests.md</c>): specifications that reach into internal
/// code live only in test projects dedicated to that — marked by <c>Internals</c> in their name, like the
/// <c>*.InternalSpecifications</c> family. Every other test project tests the public API exclusively, so a test project without
/// <c>Internals</c> in its name must receive ZERO <see cref="InternalsVisibleToAttribute"/> grants.</summary>
///<remarks>Checked at the grant, not the usage: an InternalsVisibleTo grant to an ordinary test project is a standing invitation
/// to write white-box tests there, so it is a violation even while unused.</remarks>
public static class TestProjectInternalsVisibleToPolicy
{
   [XF] public static void Only_test_projects_with_Internals_in_their_name_receive_InternalsVisibleTo_grants()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violations = AppDomain.CurrentDomain.AllCompzeAssemblies()
                                .SelectMany(assembly => assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
                                                                .Select(grant => grant.AssemblyName.Split(',')[0].Trim())
                                                                .Where(consumer => CompzeRepository.IsTestProject(consumer) && !consumer.ContainsOrdinal("Internals") && !consumer.ContainsOrdinal("InternalSpecifications"))
                                                                .Select(consumer => $"{assembly.GetName().Name} grants InternalsVisibleTo the ordinary test project {consumer}"))
                                .Distinct()
                                .Order(StringComparer.Ordinal)
                                .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }
}
