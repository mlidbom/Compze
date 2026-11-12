// ReSharper disable InconsistentNaming
using Compze.Must;
using Compze.xUnitBDD;
using PostReleaseBindingCheck.Failures;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace PostReleaseBindingCheck.Specifications.References;

/// <summary>
/// The library's source gained a method after its release, but its version was not bumped, so this release does not publish it:<br/>
/// a consumer gets the released library, without the method, together with this release's package that calls it.
/// </summary>
public class When_a_package_of_this_release_uses_a_change_the_release_does_not_publish : PostReleaseBindingCheckSpecification
{
   readonly IReadOnlyList<BindingFailure> _failures;

   public When_a_package_of_this_release_uses_a_change_the_release_does_not_publish()
   {
      var releasedLibrary = Packages.Pack(LibraryId, "1.0.0", """public class Greeter { public string Greet() => "hello"; }""");
      var buildOfTheUnreleasedLibrarySource = Packages.Pack(LibraryId, "1.0.0-unreleased-source", """public class Greeter { public string Greet() => "hello"; public string Farewell() => "bye"; }""");
      var packageOfThisRelease = Packages.Pack(ReleasedPackageId, "2.0.0", "public static class Usage { public static string Use() => new Greeter().Farewell(); }", buildOfTheUnreleasedLibrarySource);
      _failures = FailuresOf(thisRelease: [packageOfThisRelease], onNuGetOrg: [releasedLibrary]);
   }

   [XF] public void the_failure_names_the_package_of_this_release_and_the_method_the_released_library_lacks() =>
      _failures.Select(failure => failure.ToString()).Must().SequenceEqual(["ReleasedPackage 2.0.0 (this release) against Library 1.0.0 (on nuget.org): System.String Greeter::Farewell() is missing"]);
}
