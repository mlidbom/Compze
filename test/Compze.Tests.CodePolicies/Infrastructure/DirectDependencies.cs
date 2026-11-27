using System.Text.Json;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Tests.CodePolicies.Infrastructure;

///<summary>What each project and package this test project ends up with references directly, as this test project's
/// <c>.deps.json</c> records it.</summary>
///<remarks>The <c>.deps.json</c> is compiled output: it lies beside the test assembly wherever the tests run, NCrunch's workspaces
/// included, where no project file can be read (see <see cref="CompzeRepository"/>). It lists, for each project and package, the
/// projects and packages it references directly, whichever of the two a run builds a Compze library as.</remarks>
static class DirectDependencies
{
   static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ByProjectOrPackage = ReadFromThisTestProjectsDepsJson();

   ///<summary>The names of the projects and packages that <paramref name="projectOrPackageName"/> references directly.</summary>
   public static IReadOnlySet<string> Of(string projectOrPackageName)
   {
      State.Assert(ByProjectOrPackage.ContainsKey(projectOrPackageName),
                   () => $"This test project's .deps.json doesn't list {projectOrPackageName}: make the test project depend on it, directly or not, so that the policies can judge it.");
      return ByProjectOrPackage[projectOrPackageName];
   }

   static IReadOnlyDictionary<string, IReadOnlySet<string>> ReadFromThisTestProjectsDepsJson()
   {
      var depsJsonPath = Path.ChangeExtension(typeof(DirectDependencies).Assembly.Location, ".deps.json");
      using var depsJson = JsonDocument.Parse(File.ReadAllText(depsJsonPath));
      var librariesOfTheTargetFramework = depsJson.RootElement.GetProperty("targets").EnumerateObject().First().Value;
      return librariesOfTheTargetFramework.EnumerateObject()
                                          .ToDictionary(library => library.Name.Split('/')[0],
                                                        library => (IReadOnlySet<string>)NamesOfTheDependencies(library.Value),
                                                        StringComparer.Ordinal);
   }

   static HashSet<string> NamesOfTheDependencies(JsonElement library) =>
      library.TryGetProperty("dependencies", out var dependencies)
         ? dependencies.EnumerateObject().Select(dependency => dependency.Name).ToHashSet(StringComparer.Ordinal)
         : new HashSet<string>(StringComparer.Ordinal);
}
