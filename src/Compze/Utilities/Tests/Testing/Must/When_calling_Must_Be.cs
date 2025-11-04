using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_Be : UniversalTestBase
{
   public class with_two_equal_strings : When_calling_Must_Be
   {
      readonly string _actual = "same_value";
      readonly string _expected = "same_value";

      [XF] public void it_does_not_throw() => _actual.Must().Be(_expected);
   }

   public class with_two_equal_integers : When_calling_Must_Be
   {
      readonly int _actual = 42;
      readonly int _expected = 42;

      [XF] public void it_does_not_throw() => _actual.Must().Be(_expected);
   }

   public class with_two_different_integers : When_calling_Must_Be
   {
      readonly int _actual = 42;
      readonly int _expected = 43;

      public class it_throws_AssertionFailedException : with_two_different_integers
      {
         string ExceptionMessage() => Invoking(() => _actual.Must().Be(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

         [XF] public void and_the_exception_message__is() =>
            ExceptionMessage().Must().Be("""
                                         
                                         --------------------------------------------------
                                         Failing assertion:
                                         --------------------------------------------------
                                         _actual.Must().Be(_expected)
                                         --------------------------------------------------
                                         the fist failing equivalency test was: 
                                            it => Equals(it, expected)
                                         --------------------------------------------------
                                         Diff:
                                         --------------------------------------------------
                                         [-43]
                                         [+42]
                                         --------------------------------------------------
                                         "it" was:
                                         --------------------------------------------------
                                         42
                                         --------------------------------------------------
                                         "expected" was:
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

      [XF] public void Be_does_not_throw() => _actual.Must().Be(_expected);
   }

   public class given_two_different_custom_objects_with_overridden_equals : When_calling_Must_Be
   {
      readonly TestObjectWithOverriddenEquals _actual = new("actual_value");
      readonly TestObjectWithOverriddenEquals _expected = new("expected_value");

      public class Be_throws_AssertionFailedException : given_two_different_custom_objects_with_overridden_equals
      {
         string ExceptionMessage() => Invoking(() => _actual.Must().Be(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

         [XF] public void and_the_exception_message__is() =>
            ExceptionMessage().Must().Be(""""
                                         
                                         --------------------------------------------------
                                         Failing assertion:
                                         --------------------------------------------------
                                         _actual.Must().Be(_expected)
                                         --------------------------------------------------
                                         the fist failing equivalency test was: 
                                            it => Equals(it, expected)
                                         --------------------------------------------------
                                         Diff:
                                         --------------------------------------------------
                                         --- expected
                                         +++ actual
                                         @@ -1,4 +1,4 @@
                                          {
                                            "$type": "Compze.Utilities.Tests.Testing.Must.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Utilities.Tests",
                                         -  "_value": "expected_value"
                                         +  "_value": "actual_value"
                                          }
                                         
                                         --------------------------------------------------
                                         "it" was:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Utilities.Tests.Testing.Must.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Utilities.Tests",
                                           "_value": "actual_value"
                                         }
                                         --------------------------------------------------
                                         "expected" was:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Utilities.Tests.Testing.Must.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Utilities.Tests",
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
