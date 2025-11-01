using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;
// ReSharper disable NotAccessedPositionalProperty.Local

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_Satisfy : UniversalTestBase
{
   public class given_a_value_that_satisfies_the_predicate : When_using_Satisfy
   {
      readonly int _value = 5;
      [XF] public void Satisfy_returns_successfully() => _value.Must().Satisfy(v => v > 0);
   }

   public class given_a_value_that_does_not_satisfy_the_predicate : When_using_Satisfy
   {
      readonly int _value = 5;

      public class Satisfy_throws : given_a_value_that_does_not_satisfy_the_predicate
      {
         string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10))
                                     .Must()
                                     .Throw<AssertionFailedException>()
                                     .Which
                                     .Message;

         public class and_the_exception_message : Satisfy_throws
         {
            [XF] public void contains_the_expression_being_tested()
               => ExceptionMessage().Must().Contain(nameof(_value));

            [XF] public void contains_the_predicate_expression()
               => ExceptionMessage().Must().Contain("v => v > 10");
         }
      }
   }

   public class with_a_custom_error_message : When_using_Satisfy
   {
      readonly int _value = 5;

      string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10, messageOverride: _ => "Custom error message"))
                                  .Must()
                                  .Throw<AssertionFailedException>()
                                  .Which
                                  .Message;

      public class Satisfy_throws_with_custom_message : with_a_custom_error_message
      {
         public class and_the_exception_message : Satisfy_throws_with_custom_message
         {
            [XF] public void contains_the_custom_message()
               => ExceptionMessage().Must().Contain("Custom error message");

            [XF] public void does_not_contain_the_default_format()
               => ExceptionMessage().Must().Satisfy(msg => !msg.Contains("failed", StringComparison.Ordinal));
         }
      }
   }

   public class given_a_complex_object : When_using_Satisfy
   {
      readonly TestObject _actual = new("John", 30, "Unmarried");

      record TestObject(string Name, int Age, string Status);

      string ExceptionMessage() => Invoking(() => _actual.Must().Satisfy(it => it.Name == "all wrong"))
                                  .Must().Throw<AssertionFailedException>()
                                  .Which.Message;

      [XF] public void the_full_message_is() =>
         ExceptionMessage().Must().Be("""
                                      --------------------------------------------------
                                      expected the object returned by the expression:
                                      --------------------------------------------------
                                         _actual
                                      --------------------------------------------------
                                      to Satisfy:
                                      --------------------------------------------------
                                         it => it.Name == "all wrong"
                                      --------------------------------------------------
                                      but it did not
                                      --------------------------------------------------
                                      The value of: 
                                         _actual
                                      Was:
                                      --------------------------------------------------
                                      ToString():
                                      --------------------------------------------------
                                      TestObject { Name = John, Age = 30, Status = Unmarried }
                                      --------------------------------------------------
                                      JSON:
                                      --------------------------------------------------
                                      {
                                        "$type": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit",
                                        "Age": 30,
                                        "EqualityContract": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                        "Name": "John",
                                        "Status": "Unmarried"
                                      }
                                      --------------------------------------------------
                                      """);
   }
}
