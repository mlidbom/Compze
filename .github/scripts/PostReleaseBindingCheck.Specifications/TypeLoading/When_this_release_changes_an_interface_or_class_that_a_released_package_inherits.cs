// ReSharper disable InconsistentNaming
using Compze.Must;
using Compze.xUnitBDD;
using PostReleaseBindingCheck.Failures;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace PostReleaseBindingCheck.Specifications.TypeLoading;

public class When_this_release_changes_an_interface_or_class_that_a_released_package_inherits : PostReleaseBindingCheckSpecification
{
   const string ReleasedGreeterInterface = "public interface IGreeter { string Greet(); }";
   const string ReleasedPackageImplementingTheGreeterInterface = """public class Greeter : IGreeter { public string Greet() => "hello"; }""";

   public class without_changing_the_interfaces_and_classes_the_released_package_inherits : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      const string Library = """
                             public interface IGreeter { string Greet(); }
                             public interface IHandler<T> { void Handle(T message); }
                             public abstract class Responder<T> { public abstract T Respond(T message); }
                             """;

      readonly IReadOnlyList<BindingFailure> _failures;

      public without_changing_the_interfaces_and_classes_the_released_package_inherits() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: Library,
         releasedPackage: """
                          public class ExplicitGreeter : IGreeter { string IGreeter.Greet() => "hello"; }
                          public class StringHandler : IHandler<string> { public void Handle(string message) { } }
                          public class GenericHandler<T> : IHandler<T> { public void Handle(T message) { } }
                          public class StringResponder : Responder<string> { public override string Respond(string message) => message; }
                          """,
         libraryInThisRelease: Library + "public class Farewells { }");

      [XF] public void the_check_finds_no_failures_in_explicit_generic_or_overriding_implementations() => _failures.Must().BeEmpty();
   }

   public class by_adding_a_method_to_an_interface_the_released_package_implements : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_adding_a_method_to_an_interface_the_released_package_implements() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterInterface,
         releasedPackage: ReleasedPackageImplementingTheGreeterInterface,
         libraryInThisRelease: "public interface IGreeter { string Greet(); string Farewell(); }");

      [XF] public void the_implementing_class_is_reported_to_not_implement_the_new_method() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["UnimplementedAbstractMember: Greeter does not implement IGreeter::Farewell, instance System.String()`0"]);
   }

   public class by_adding_a_method_with_a_default_implementation_to_an_interface_the_released_package_implements
      : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_adding_a_method_with_a_default_implementation_to_an_interface_the_released_package_implements() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterInterface,
         releasedPackage: ReleasedPackageImplementingTheGreeterInterface,
         libraryInThisRelease: """public interface IGreeter { string Greet(); string Farewell() => "bye"; }""");

      [XF] public void the_check_finds_no_failures() => _failures.Must().BeEmpty();
   }

   public class by_adding_a_method_to_a_generic_interface_the_released_package_implements : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_adding_a_method_to_a_generic_interface_the_released_package_implements() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: "public interface IHandler<T> { void Handle(T message); }",
         releasedPackage: "public class StringHandler : IHandler<string> { public void Handle(string message) { } }",
         libraryInThisRelease: "public interface IHandler<T> { void Handle(T message); void Reject(T message); }");

      [XF] public void the_implementing_class_is_reported_to_not_implement_the_new_method_with_its_type_argument() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["UnimplementedAbstractMember: StringHandler does not implement IHandler`1<System.String>::Reject, instance System.Void(System.String)`0"]);
   }

   public class by_giving_an_interface_the_released_package_implements_a_base_interface : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_giving_an_interface_the_released_package_implements_a_base_interface() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterInterface,
         releasedPackage: ReleasedPackageImplementingTheGreeterInterface,
         libraryInThisRelease: """
                               public interface IPolite { string Thank(); }
                               public interface IGreeter : IPolite { string Greet(); }
                               """);

      [XF] public void the_implementing_class_is_reported_to_not_implement_the_method_of_the_base_interface() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["UnimplementedAbstractMember: Greeter does not implement IPolite::Thank, instance System.String()`0"]);
   }

   public class by_adding_a_method_to_an_interface_an_abstract_class_of_the_released_package_implements
      : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_adding_a_method_to_an_interface_an_abstract_class_of_the_released_package_implements() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: ReleasedGreeterInterface,
         releasedPackage: """
                          public abstract class GreeterBase : IGreeter { public abstract string Greet(); }
                          public class EnglishGreeter : GreeterBase { public override string Greet() => "hello"; }
                          """,
         libraryInThisRelease: "public interface IGreeter { string Greet(); string Farewell(); }");

      [XF] public void the_concrete_class_derived_from_the_abstract_class_is_reported_to_not_implement_the_new_method() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["UnimplementedAbstractMember: EnglishGreeter does not implement IGreeter::Farewell, instance System.String()`0"]);
   }

   public class by_adding_an_abstract_method_to_a_class_the_released_package_derives_from : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_adding_an_abstract_method_to_a_class_the_released_package_derives_from() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: "public abstract class Greeter { public abstract string Greet(); }",
         releasedPackage: """public class EnglishGreeter : Greeter { public override string Greet() => "hello"; }""",
         libraryInThisRelease: "public abstract class Greeter { public abstract string Greet(); public abstract string Farewell(); }");

      [XF] public void the_derived_class_is_reported_to_not_override_the_new_method() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["UnimplementedAbstractMember: EnglishGreeter does not override Greeter::Farewell, instance System.String()`0"]);
   }

   public class by_sealing_a_class_the_released_package_derives_from : When_this_release_changes_an_interface_or_class_that_a_released_package_inherits
   {
      readonly IReadOnlyList<BindingFailure> _failures;

      public by_sealing_a_class_the_released_package_derives_from() => _failures = FailuresWhenThisReleaseChangesTheLibrary(
         releasedLibrary: "public class Greeter { }",
         releasedPackage: "public class EnglishGreeter : Greeter { }",
         libraryInThisRelease: "public sealed class Greeter { }");

      [XF] public void the_derived_class_is_reported_to_derive_from_a_sealed_class() =>
         KindsAndExplanations(_failures).Must().SequenceEqual(["SealedBaseClass: EnglishGreeter derives from Greeter, which is sealed"]);
   }
}
