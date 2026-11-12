using PostReleaseBindingCheck.Failures;
using PostReleaseBindingCheck.Packages;
using PostReleaseBindingCheck.Specifications.Fixtures;

namespace PostReleaseBindingCheck.Specifications;

/// <summary>
/// The base of the post-release binding check's specifications: makes their <see cref="FixturePackages"/>, and checks the<br/>
/// <see cref="PostReleasePackageSet"/> they make.
/// </summary>
public abstract class PostReleaseBindingCheckSpecification : IDisposable
{
   protected const string LibraryId = "Library";
   protected const string ReleasedPackageId = "ReleasedPackage";

   protected FixturePackages Packages { get; } = new();

   protected static IReadOnlyList<BindingFailure> FailuresOf(IEnumerable<FixturePackage> thisRelease, IEnumerable<FixturePackage> onNuGetOrg)
   {
      using var packageSet = PostReleasePackageSet.Load(thisRelease.Select(package => package.NupkgPath), onNuGetOrg.Select(package => package.NupkgPath));
      return packageSet.FindBindingFailures();
   }

   /// <summary>
   /// The case the check exists for: package <see cref="ReleasedPackageId"/>, compiled against <paramref name="releasedLibrary"/>, version 1.0.0 of<br/>
   /// package <see cref="LibraryId"/>, checked against <paramref name="libraryInThisRelease"/>, the version 2.0.0 this release publishes.
   /// </summary>
   protected IReadOnlyList<BindingFailure> FailuresWhenThisReleaseChangesTheLibrary(string releasedLibrary, string releasedPackage, string libraryInThisRelease)
   {
      var releasedVersionOfTheLibrary = Packages.Pack(LibraryId, "1.0.0", releasedLibrary);
      var packageUsingTheLibrary = Packages.Pack(ReleasedPackageId, "1.0.0", releasedPackage, releasedVersionOfTheLibrary);
      var versionOfTheLibraryInThisRelease = Packages.Pack(LibraryId, "2.0.0", libraryInThisRelease);
      return FailuresOf(thisRelease: [versionOfTheLibraryInThisRelease], onNuGetOrg: [packageUsingTheLibrary]);
   }

   /// <summary>Each failure as <c>Kind: Explanation</c>, such as <c>MissingMember: System.String Greeter::Greet() is missing</c>.</summary>
   protected static IReadOnlyList<string> KindsAndExplanations(IEnumerable<BindingFailure> failures) => [..failures.Select(failure => $"{failure.Kind}: {failure.Explanation}")];

   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

   protected virtual void Dispose(bool disposing)
   {
      if(disposing) Packages.Dispose();
   }
}
