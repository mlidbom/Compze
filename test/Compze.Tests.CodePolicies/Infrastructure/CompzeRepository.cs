using System.Xml.Linq;
using Compze.Contracts;
using Compze.Internals.SystemCE;
using static Compze.Contracts.Contract;

namespace Compze.Tests.CodePolicies.Infrastructure;

///<summary>The repository the running tests were built from: which projects it contains, whether each is a library or a test
/// project, and where its sources live — the facts the code policies judge the compiled assemblies against.</summary>
///<remarks>Every one of those facts is read from <c>Compze.AllProjects.slnx</c>, never from the directories around the test
/// assembly. The distinction matters because the tests do not always run where the repository is. NCrunch builds and runs each
/// project in a workspace holding compiled output and a short list of copied files, and it may ship that workspace to a grid node
/// on another machine entirely, where no path into the developer's working tree exists at all. A policy that answered "is this a
/// test project?" by probing for its <c>.csproj</c> would answer no to every project there — and then pass, having examined
/// nothing. The solution file is one file, it already names every project and whether it sits under <c>src</c> or <c>test</c>, and
/// the project's <c>.ncrunchproject</c> already copies it into the workspace, so it travels wherever the tests do.</remarks>
static class CompzeRepository
{
   const string SolutionFileName = "Compze.AllProjects.slnx";

   ///<summary>The directory holding <see cref="SolutionFileName"/>. In a normal run this is the repository root; in an NCrunch
   /// workspace it is the workspace, which holds the copied solution file but no source tree — so only ask this for sources after
   /// <see cref="HasSourceTree"/> says there is one.</summary>
   static string Root { get; } = FindRoot();

   ///<summary>Every project in the solution, as the solution spells it: <c>src/Name/Name.csproj</c> or <c>test/Name/Name.csproj</c>.</summary>
   static readonly IReadOnlySet<string> ProjectPaths = ReadProjectPathsFromSolution();

   ///<summary>True when this repository's sources are reachable from <see cref="Root"/> — false in an NCrunch workspace, which
   /// receives compiled output rather than a source tree.</summary>
   static bool HasSourceTree { get; } = Directory.Exists(Path.Combine(Root, "src")) && Directory.Exists(Path.Combine(Root, "test"));

   ///<summary>A folder of this repository's sources, for the policies that read source text rather than compiled metadata.</summary>
   ///<remarks>Throws where the sources are not reachable rather than returning a path that does not exist. A policy that reads
   /// source files finds none in an NCrunch workspace and would otherwise report success having read nothing — the silent pass
   /// this whole type exists to make impossible. Such a policy either needs its sources copied into the workspace (see this
   /// project's <c>.ncrunchproject</c>) or needs to be rewritten to judge compiled metadata like every other policy here.</remarks>
   public static string SourceFolder(params string[] pathWithinTheRepository)
   {
      State.Assert(HasSourceTree,
                   () => $"{Root} holds {SolutionFileName} but no src and test directories, so the tests are running from a copy of the repository rather than from the repository itself, and no source file is reachable.");
      return Path.Combine([Root, .. pathWithinTheRepository]);
   }

   ///<summary>The names of every project the solution places under <c>src/</c> — the shipped libraries.</summary>
   public static IEnumerable<string> LibraryProjectNames => ProjectNamesUnder("src");

   ///<summary>The names of every project the solution places under <c>test/</c>.</summary>
   public static IEnumerable<string> TestProjectNames => ProjectNamesUnder("test");

   ///<summary>True when <paramref name="projectName"/> is a test project — the solution places it under <c>test/</c>.</summary>
   public static bool IsTestProject(string projectName) => ProjectPaths.Contains($"test/{projectName}/{projectName}.csproj");

   ///<summary>True when <paramref name="projectName"/> is any project in this repository — under <c>src/</c> or <c>test/</c>.</summary>
   public static bool IsRepoProject(string projectName) => ProjectPaths.Contains($"src/{projectName}/{projectName}.csproj") || IsTestProject(projectName);

   ///<summary>True when <paramref name="projectName"/> is a test project that declares itself white-box — the <c>*.InternalSpecifications</c>
   /// and <c>*.Internals</c> families, the only test projects allowed to reach past a library's public API.</summary>
   ///<remarks>Matched on the name's ending, not on containing <c>Internals</c> anywhere: the <c>Compze.Internals.*</c> packages put that
   /// word in the middle of the names of their perfectly ordinary black-box specification projects.</remarks>
   public static bool IsWhiteBoxTestProject(string projectName) =>
      IsTestProject(projectName) && (projectName.EndsWithOrdinal("InternalSpecifications") || projectName.EndsWithOrdinal(".Internals"));

   ///<summary>The names of the projects <paramref name="folder"/> holds in the repository's flat layout — <c>folder/Name/Name.csproj</c>,
   /// one top-level directory per project.</summary>
   ///<remarks>Projects nested deeper than that are deliberately skipped. They are the build's own scaffolding — the solution-structure
   /// validators, the website, the msbuild tasks — not Compze libraries, and the code policies neither load nor govern them.</remarks>
   static IEnumerable<string> ProjectNamesUnder(string folder) =>
      ProjectPaths.Select(path => path.Split('/'))
                  .Where(segments => segments.Length == 3
                                  && string.Equals(segments[0], folder, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(segments[1], Path.GetFileNameWithoutExtension(segments[2]), StringComparison.OrdinalIgnoreCase))
                  .Select(segments => segments[1]);

   static IReadOnlySet<string> ReadProjectPathsFromSolution()
   {
      var projectPaths = XDocument.Load(Path.Combine(Root, SolutionFileName))
                                  .Descendants("Project")
                                  .Select(project => project.Attribute("Path")!.Value.Replace('\\', '/'))
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

      State.Assert(projectPaths.Count > 0, () => $"{Path.Combine(Root, SolutionFileName)} declares no projects, so every policy that asks what a project is would answer no and pass having examined nothing.");
      return projectPaths;
   }

   static string FindRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);
      while(directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
         directory = directory.Parent;

      State.Assert(directory is not null, () => $"Found no {SolutionFileName} above {AppContext.BaseDirectory}. A test runner that copies the assembly out of the repository must copy the solution file with it - see this project's .ncrunchproject.");
      return directory.FullName;
   }
}
