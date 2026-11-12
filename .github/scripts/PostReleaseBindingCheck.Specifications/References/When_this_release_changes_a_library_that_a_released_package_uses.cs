// ReSharper disable InconsistentNaming
using Compze.Must;
using Compze.xUnitBDD;
using PostReleaseBindingCheck.Failures;
using PostReleaseBindingCheck.Specifications.Fixtures;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace PostReleaseBindingCheck.Specifications.References;

public class When_this_release_changes_a_library_that_a_released_package_uses : PostReleaseBindingCheckSpecification
{
   const string ReleasedGreeterLibrary = """public class Greeter { public string Greet() => "hello"; }""";
   const string ReleasedPackageCallingGreet = "public static class Usage { public static string Use() => new Greeter().Greet(); }";

   public class without_changing_anything_the_released_package_uses : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public without_changing_anything_the_released_package_uses() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """
                               public class Greeter { public string Greet() => "hi"; public string Farewell() => "bye"; }
                               public class Farewells { }
                               """);

      [XF] public void the_check_finds_no_failures() => _failures.Must().BeEmpty();
   }

   public class by_removing_a_method_the_released_package_calls : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_removing_a_method_the_released_package_calls() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: "public class Greeter { }");

      [XF] public void the_method_is_reported_missing() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: System.String Greeter::Greet() is missing"]);

      [XF] public void the_failure_names_the_released_package_and_the_library_of_this_release() =>
         _failures.Single().ToString().Must().Be("ReleasedPackage 1.0.0 (on nuget.org) against Library 2.0.0 (this release): System.String Greeter::Greet() is missing");
   }

   public class by_removing_a_type_the_released_package_uses : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_removing_a_type_the_released_package_uses() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: "public class Farewells { }");

      [XF] public void the_type_is_reported_missing_once_and_not_the_members_used_on_it() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingType: type Greeter is missing"]);
   }

   public class by_changing_the_type_of_a_parameter_of_a_method_the_released_package_calls : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_changing_the_type_of_a_parameter_of_a_method_the_released_package_calls() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: """public class Greeter { public string Greet(string name) => "hello " + name; }""",
         releasedPackage: """public static class Usage { public static string Use() => new Greeter().Greet("Ada"); }""",
         libraryInThisRelease: """public class Greeter { public string Greet(int times) => "hello"; }""");

      [XF] public void the_method_with_the_old_signature_is_reported_missing() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: System.String Greeter::Greet(System.String) is missing"]);
   }

   public class by_changing_the_return_type_of_a_method_the_released_package_calls : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_changing_the_return_type_of_a_method_the_released_package_calls() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """public class Greeter { public object Greet() => "hello"; }""");

      [XF] public void the_method_with_the_old_signature_is_reported_missing() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: System.String Greeter::Greet() is missing"]);
   }

   public class by_making_an_instance_method_the_released_package_calls_static : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_making_an_instance_method_the_released_package_calls_static() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """public class Greeter { public static string Greet() => "hello"; }""");

      [XF] public void the_instance_method_is_reported_missing() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: System.String Greeter::Greet() is missing"]);
   }

   public class by_moving_a_method_the_released_package_calls_to_a_base_class : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_moving_a_method_the_released_package_calls_to_a_base_class() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """
                               public class GreeterBase { public string Greet() => "hello"; }
                               public class Greeter : GreeterBase { }
                               """);

      [XF] public void the_check_finds_no_failures_since_the_runtime_finds_the_method_on_the_base_class() => _failures.Must().BeEmpty();
   }

   public class by_removing_a_field_the_released_package_reads : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_removing_a_field_the_released_package_reads() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: "public class Settings { public int Retries = 3; }",
         releasedPackage: "public static class Usage { public static int Use() => new Settings().Retries; }",
         libraryInThisRelease: "public class Settings { }");

      [XF] public void the_field_is_reported_missing() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: System.Int32 Settings::Retries is missing"]);
   }

   public class by_removing_a_method_of_a_generic_class_the_released_package_calls : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_removing_a_method_of_a_generic_class_the_released_package_calls() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: "public class Box<T> { public T? Get() => default; }",
         releasedPackage: "public static class Usage { public static int Use() => new Box<int>().Get(); }",
         libraryInThisRelease: "public class Box<T> { }");

      [XF] public void the_method_is_reported_missing_on_the_generic_class_the_released_package_instantiates() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["MissingMember: !0 Box`1<System.Int32>::Get() is missing"]);
   }

   public class by_making_a_method_the_released_package_calls_internal : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_making_a_method_the_released_package_calls_internal() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """public class Greeter { internal string Greet() => "hello"; }""");

      [XF] public void the_method_is_reported_inaccessible_to_the_released_package() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["InaccessibleMember: System.String Greeter::Greet() is not accessible to ReleasedPackage"]);
   }

   public class by_making_a_method_the_released_package_calls_internal_and_granting_the_released_package_InternalsVisibleTo
      : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_making_a_method_the_released_package_calls_internal_and_granting_the_released_package_InternalsVisibleTo() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """
                               [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReleasedPackage")]
                               public class Greeter { internal string Greet() => "hello"; }
                               """);

      [XF] public void the_check_finds_no_failures() => _failures.Must().BeEmpty();
   }

   public class by_making_a_type_the_released_package_uses_internal : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_making_a_type_the_released_package_uses_internal() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterLibrary,
         releasedPackage: ReleasedPackageCallingGreet,
         libraryInThisRelease: """internal class Greeter { public string Greet() => "hello"; }""");

      [XF] public void the_type_is_reported_inaccessible_to_the_released_package() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["InaccessibleType: type Greeter is not accessible to ReleasedPackage"]);
   }

   public class by_moving_a_type_the_released_package_uses_to_another_package_of_this_release : When_this_release_changes_a_library_that_a_released_package_uses
   {
      readonly FixturePackage _releasedPackage;
      readonly FixturePackage _packageTheTypeMovesTo;

      public by_moving_a_type_the_released_package_uses_to_another_package_of_this_release()
      {
         var releasedVersionOfTheLibrary = Packages.Pack(LibraryId, "1.0.0", ReleasedGreeterLibrary);
         _releasedPackage = Packages.Pack(ReleasedPackageId, "1.0.0", ReleasedPackageCallingGreet, releasedVersionOfTheLibrary);
         _packageTheTypeMovesTo = Packages.Pack("Greetings", "1.0.0", ReleasedGreeterLibrary);
      }

      public class with_a_type_forwarder_in_the_library : by_moving_a_type_the_released_package_uses_to_another_package_of_this_release
      {
         readonly IReadOnlyList<BindingFailure> _failures;

         public with_a_type_forwarder_in_the_library()
         {
            var libraryInThisRelease = Packages.Pack(LibraryId, "2.0.0", "[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Greeter))]", _packageTheTypeMovesTo);
            _failures = FailuresOf(thisRelease: [libraryInThisRelease, _packageTheTypeMovesTo], onNuGetOrg: [_releasedPackage]);
         }

         [XF] public void the_check_finds_no_failures_since_the_runtime_follows_the_forwarder() => _failures.Must().BeEmpty();
      }

      public class without_a_type_forwarder_in_the_library : by_moving_a_type_the_released_package_uses_to_another_package_of_this_release
      {
         readonly IReadOnlyList<BindingFailure> _failures;

         public without_a_type_forwarder_in_the_library()
         {
            var libraryInThisRelease = Packages.Pack(LibraryId, "2.0.0", "public class Farewells { }");
            _failures = FailuresOf(thisRelease: [libraryInThisRelease, _packageTheTypeMovesTo], onNuGetOrg: [_releasedPackage]);
         }

         [XF] public void the_type_is_reported_missing_from_the_library() =>
            KindsAndExplanations(_failures).Must().SequenceEqual(["MissingType: type Greeter is missing"]);
      }
   }
}
