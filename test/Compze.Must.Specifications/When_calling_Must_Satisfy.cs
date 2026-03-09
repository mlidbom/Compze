

// ReSharper disable NotAccessedPositionalProperty.Local

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_Satisfy : UniversalTestBase
{
   public class with_a_predicate_returning_true : When_calling_Must_Satisfy
   {
      readonly int _value = 5;
      [XF] public void it_does_not_throw() => _value.Must().Satisfy(v => v > 0);
   }

   public class with_a_predicate_returning_false : When_calling_Must_Satisfy
   {
      readonly int _value = 5;

      public class it_throws_AssertionFailedException_ : with_a_predicate_returning_false
      {
         string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10))
                                     .Must()
                                     .Throw<AssertionFailedException>()
                                     .Which
                                     .Message;

         public class and_the_exception_message : it_throws_AssertionFailedException_
         {
            [XF] public void contains_the_expression_being_tested()
               => ExceptionMessage().Must().Contain(nameof(_value));

            [XF] public void contains_the_predicate_expression()
               => ExceptionMessage().Must().Contain("v => v > 10");
         }
      }
   }

   public class using_a_custom_error_message : When_calling_Must_Satisfy
   {
      readonly int _value = 5;

      string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10, failureMessage: _ => "Custom error message"))
                                  .Must()
                                  .Throw<AssertionFailedException>()
                                  .Which
                                  .Message;

      public class and_the_exception_message : using_a_custom_error_message
      {
         [XF] public void contains_the_custom_message()
            => ExceptionMessage().Must().Contain("Custom error message");
      }
   }

   public class given_a_complex_object_as_actual_and_a_predicate_that_returns_false : When_calling_Must_Satisfy
   {
      readonly TestObject _actual = new("John", 30, "Unmarried");

      record TestObject(string Name, int Age, string Status);

      string ExceptionMessage() => Invoking(() => _actual.Must().Satisfy(it => it.Name == "all wrong"))
                                  .Must().Throw<AssertionFailedException>()
                                  .Which.Message;

      [XF] public void it_throws_and_the_full_message_is() =>
         ExceptionMessage().Must().Be("""
                                      
                                      --------------------------------------------------
                                      Failing assertion:
                                      --------------------------------------------------
                                      _actual.Must().Satisfy(it => it.Name == "all wrong")
                                      --------------------------------------------------
                                      _actual was a Compze.Must.Specifications.When_calling_Must_Satisfy.given_a_complex_object_as_actual_and_a_predicate_that_returns_false.TestObject with:
                                      --------------------------------------------------
                                      ToString():
                                      --------------------------------------------------
                                      TestObject { Name = John, Age = 30, Status = Unmarried }
                                      --------------------------------------------------
                                      JSON:
                                      --------------------------------------------------
                                      {
                                        "$type": "Compze.Must.Specifications.When_calling_Must_Satisfy+given_a_complex_object_as_actual_and_a_predicate_that_returns_false+TestObject, Compze.Must.Specifications",
                                        "Age": 30,
                                        "EqualityContract": "Compze.Must.Specifications.When_calling_Must_Satisfy+given_a_complex_object_as_actual_and_a_predicate_that_returns_false+TestObject, Compze.Must.Specifications, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                        "Name": "John",
                                        "Status": "Unmarried"
                                      }
                                      --------------------------------------------------
                                      """);
   }
}
