using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_Satisfy : UniversalTestBase
{
   public class given_a_value_that_satisfies_the_predicate : When_using_Satisfy
   {
      readonly int _value = 5;

      public class Satisfy_returns_successfully : given_a_value_that_satisfies_the_predicate
      {
         [XF] public void when_predicate_returns_true() => _value.Must().Satisfy(v => v > 0);
      }

      public class and_allows_chaining : given_a_value_that_satisfies_the_predicate
      {
         [XF] public void so_multiple_assertions_can_be_made()
            => _value.Must()
                     .Satisfy(v => v > 0)
                     .Satisfy(v => v < 10)
                     .Satisfy(v => v % 5 == 0);
      }
   }

   public class given_a_value_that_does_not_satisfy_the_predicate : When_using_Satisfy
   {
      readonly int _value = 5;

      public class Satisfy_throws : given_a_value_that_does_not_satisfy_the_predicate
      {
         string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10))
                                     .Must()
                                     .Throw<AssertionFailedException>()
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

      string ExceptionMessage() => Invoking(() => _value.Must().Satisfy(v => v > 10, () => "Custom error message"))
                                  .Must()
                                  .Throw<AssertionFailedException>()
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
                                  .Must()
                                  .Throw<AssertionFailedException>()
                                  .Message;

      [XF] public void the_message_contains_the_full_json_for_actual() =>
         ExceptionMessage().Must().Contain("""
                                           Actual JSON:
                                           --------------------------------------------------
                                           {
                                             "$type": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit",
                                             "EqualityContract": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                             "Name": "John",
                                             "Age": 30,
                                             "Status": "Unmarried"
                                           }
                                           --------------------------------------------------
                                           """);

      [XF] public void the_full_message_is() =>
         ExceptionMessage().Must().Be("""
                                        
                                        expected the expression:
                                        --------------------------------------------------
                                           _actual
                                        --------------------------------------------------
                                        to Satisfy:
                                        --------------------------------------------------
                                           it => it.Name == "all wrong"
                                        --------------------------------------------------
                                        
                                        Actual.ToString():
                                        --------------------------------------------------
                                           TestObject { Name = John, Age = 30, Status = Unmarried }
                                        --------------------------------------------------
                                        
                                        Actual JSON:
                                        --------------------------------------------------
                                        {
                                          "$type": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit",
                                          "EqualityContract": "Compze.Tests.Unit.Testing.Fluent.When_using_Satisfy+given_a_complex_object+TestObject, Compze.Tests.Unit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                          "Name": "John",
                                          "Age": 30,
                                          "Status": "Unmarried"
                                        }
                                        --------------------------------------------------
                                        
                                        """);
   }
}
