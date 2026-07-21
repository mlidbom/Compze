using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Tests.CodePolicies;

///<summary>Locates the Compze repository the running tests were built from, so code policies can inspect its sources and project layout.</summary>
static class CompzeRepository
{
   ///<summary>The repository root: the directory holding <c>Compze.AllProjects.slnx</c>, found by walking up from the test assembly's own location.</summary>
   public static string Root { get; } = FindRoot();

   ///<summary>True when <paramref name="projectName"/> is a test project in this repository — a <c>test/&lt;name&gt;/&lt;name&gt;.csproj</c> exists.</summary>
   public static bool IsTestProject(string projectName) =>
      File.Exists(Path.Combine(Root, "test", projectName, projectName + ".csproj"));

   ///<summary>True when <paramref name="projectName"/> is any project in this repository — under <c>src/</c> or <c>test/</c>.</summary>
   public static bool IsRepoProject(string projectName) =>
      File.Exists(Path.Combine(Root, "src", projectName, projectName + ".csproj")) || IsTestProject(projectName);

   static string FindRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);
      while(directory is not null && !File.Exists(Path.Combine(directory.FullName, "Compze.AllProjects.slnx")))
         directory = directory.Parent;
      State.Assert(directory is not null, () => $"Found no Compze.AllProjects.slnx above {AppContext.BaseDirectory}.");
      return directory!.FullName;
   }
}
