using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using System;
using Compze.Utilities.Testing.Fluent;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_Be_NotBe = Compze.Utilities.Testing.Fluent.Must_Be_NotBe;
using Must_Be_string = Compze.Utilities.Testing.Fluent.Must_Be_string;

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_Be : UniversalTestBase
{
   public class with_two_equal_strings : When_calling_Must_Be
   {
      readonly string _actual = "same_value";
      readonly string _expected = "same_value";

      [XF] public void it_does_not_throw() => Must_Be_string.Be(__Must.Must(_actual), _expected);
   }

   public class with_two_equal_integers : When_calling_Must_Be
   {
      readonly int _actual = 42;
      readonly int _expected = 42;

      [XF] public void it_does_not_throw() => Must_Be_NotBe.Be(__Must.Must(_actual), _expected);
   }

   public class with_two_different_integers : When_calling_Must_Be
   {
      readonly int _actual = 42;
      readonly int _expected = 43;

      public class it_throws_AssertionFailedException : with_two_different_integers
      {
         string ExceptionMessage() => Invoking(() => Must_Be_NotBe.Be(__Must.Must(_actual), _expected)).Must().Throw<AssertionFailedException>().Which.Message;

         [XF] public void and_the_exception_message__is() =>
            Must_Be_string.Be(__Must.Must(ExceptionMessage()),
                              """

                              --------------------------------------------------
                              expected the object "it" returned by the expression: 
                              --------------------------------------------------
                                 _actual
                              --------------------------------------------------
                              to be equal to the the object "expected" returned by the expression:
                              --------------------------------------------------
                                 _expected
                              --------------------------------------------------
                              but it failed the test: 
                                 it => Equals(it, expected)
                              --------------------------------------------------
                              Diff:
                              --------------------------------------------------
                              [-43]
                              [+42]
                              --------------------------------------------------
                              it was:
                              --------------------------------------------------
                              42
                              --------------------------------------------------
                              expected was:
                              --------------------------------------------------
                              43
                              --------------------------------------------------
                              """);
      }
   }

   public class given_two_equal_custom_objects_with_overridden_equals : When_calling_Must_Be
   {
      readonly TestObjectWithOverriddenEquals _actual = new("same");
      readonly TestObjectWithOverriddenEquals _expected = new("same");

      [XF] public void Be_does_not_throw() => Must_Be_NotBe.Be(__Must.Must(_actual), _expected);
   }

   public class given_two_different_custom_objects_with_overridden_equals : When_calling_Must_Be
   {
      readonly TestObjectWithOverriddenEquals _actual = new("actual_value");
      readonly TestObjectWithOverriddenEquals _expected = new("expected_value");

      public class Be_throws_AssertionFailedException : given_two_different_custom_objects_with_overridden_equals
      {
         string ExceptionMessage() => Invoking(() => Must_Be_NotBe.Be(__Must.Must(_actual), _expected)).Must().Throw<AssertionFailedException>().Which.Message;

         [XF] public void and_the_exception_message__is() =>
            ExceptionMessage().Must().Be(""""
                                         
                                         --------------------------------------------------
                                         expected the object "it" returned by the expression: 
                                         --------------------------------------------------
                                            _actual
                                         --------------------------------------------------
                                         to be equal to the the object "expected" returned by the expression:
                                         --------------------------------------------------
                                            _expected
                                         --------------------------------------------------
                                         but it failed the test: 
                                            it => Equals(it, expected)
                                         --------------------------------------------------
                                         Diff:
                                         --------------------------------------------------
                                         --- expected
                                         +++ actual
                                         @@ -1,4 +1,4 @@
                                          {
                                            "$type": "Compze.Tests.Unit.Testing.Fluent.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Tests.Unit",
                                         -  "_value": "expected_value"
                                         +  "_value": "actual_value"
                                          }

                                         --------------------------------------------------
                                         it was:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Tests.Unit.Testing.Fluent.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Tests.Unit",
                                           "_value": "actual_value"
                                         }
                                         --------------------------------------------------
                                         expected was:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Tests.Unit.Testing.Fluent.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Tests.Unit",
                                           "_value": "expected_value"
                                         }
                                         --------------------------------------------------
                                         """");
      }
   }

   class TestObjectWithOverriddenEquals(string value)
   {
      readonly string _value = value;

      public override bool Equals(object? obj) =>
         obj is TestObjectWithOverriddenEquals other && _value == other._value;

      public override int GetHashCode() => _value.GetHashCode(StringComparison.Ordinal);

      public string GetValue() => _value;
   }
}
