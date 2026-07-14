

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

using Compze.Must.Assertions;

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Must.Specifications;

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
                                         Expected 43 but got 42
                                         --------------------------------------------------
                                         _actual was a System.Int32 with the value: 42
                                         --------------------------------------------------
                                         _expected was a System.Int32 with the value: 43
                                         --------------------------------------------------
                                         the first failing equivalency test was: 
                                            it => Equals(it, expected)
                                         --------------------------------------------------
                                         """);
      }
   }

   public class with_an_int_and_a_long_with_the_same_value : When_calling_Must_Be
   {
      readonly int _actual = 42;
      readonly long _expected = 42;

      public class it_throws_AssertionFailedException : with_an_int_and_a_long_with_the_same_value
      {
         string ExceptionMessage() => Invoking(() => _actual.Must().Be(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

         [XF] public void and_the_exception_message__is() =>
            ExceptionMessage().Must().Be("""
                                         
                                         --------------------------------------------------
                                         Failing assertion:
                                         --------------------------------------------------
                                         _actual.Must().Be(_expected)
                                         --------------------------------------------------
                                         Expected 42 but got 42
                                         --------------------------------------------------
                                         _actual was a System.Int32 with the value: 42
                                         --------------------------------------------------
                                         _expected was a System.Int64 with the value: 42
                                         --------------------------------------------------
                                         the first failing equivalency test was: 
                                            it => Equals(it, expected)
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
            ExceptionMessage().Must().Be("""
                                         
                                         --------------------------------------------------
                                         Failing assertion:
                                         --------------------------------------------------
                                         _actual.Must().Be(_expected)
                                         --------------------------------------------------
                                         Diff:
                                         --------------------------------------------------
                                         --- expected
                                         +++ actual
                                         @@ -1,4 +1,4 @@
                                          {
                                            "$type": "Compze.Must.Specifications.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Must.Specifications",
                                         -  "_value": "expected_value"
                                         +  "_value": "actual_value"
                                          }
                                         
                                         --------------------------------------------------
                                         _actual was a Compze.Must.Specifications.When_calling_Must_Be.TestObjectWithOverriddenEquals with:
                                         --------------------------------------------------
                                         JSON:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Must.Specifications.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Must.Specifications",
                                           "_value": "actual_value"
                                         }
                                         --------------------------------------------------
                                         _expected was a Compze.Must.Specifications.When_calling_Must_Be.TestObjectWithOverriddenEquals with:
                                         --------------------------------------------------
                                         JSON:
                                         --------------------------------------------------
                                         {
                                           "$type": "Compze.Must.Specifications.When_calling_Must_Be+TestObjectWithOverriddenEquals, Compze.Must.Specifications",
                                           "_value": "expected_value"
                                         }
                                         --------------------------------------------------
                                         the first failing equivalency test was: 
                                            it => Equals(it, expected)
                                         --------------------------------------------------
                                         """);
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
