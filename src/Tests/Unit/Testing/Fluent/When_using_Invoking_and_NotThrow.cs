using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_Invoking_and_NotThrow : UniversalTestBase
{
   public class given_an_action_that_does_not_throw : When_using_Invoking_and_NotThrow
   {
      [XF] public void NotThrow_succeeds()
         => Invoking(() =>
            {
               // do nothing
            })
           .Must()
           .NotThrow();

      [XF] public void NotThrow_returns_the_Must_for_chaining()
      {
         var result = Invoking(() =>
                      {
                         // do nothing
                      })
                     .Must()
                     .NotThrow();

         result.Must().Satisfy(it => it is not null);
      }
   }

   public class given_an_action_that_throws_an_exception : When_using_Invoking_and_NotThrow
   {
      public class NotThrow_throws : given_an_action_that_throws_an_exception
      {
         [XF] public void when_any_exception_is_thrown()
            => Invoking(() => Invoking(() => throw new InvalidOperationException("error"))
                             .Must()
                             .NotThrow())
              .Must()
              .Throw<AssertionFailedException>();

         public class and_the_exception_message : NotThrow_throws
         {
            string ExceptionMessage() => Invoking(() => Invoking(() => throw new InvalidOperationException("test error"))
                                                       .Must()
                                                       .NotThrow())
                                        .Must()
                                        .Throw<AssertionFailedException>()
                                        .Which
                                        .Message;

            [XF] public void contains_the_expression()
               => ExceptionMessage().Must().Contain("() => throw new InvalidOperationException(\"test error\")");

            [XF] public void contains_the_exception_type()
               => ExceptionMessage().Must().Contain("System.InvalidOperationException");

            [XF] public void contains_the_exception_message()
               => ExceptionMessage().Must().Contain("test error");

            [XF] public void indicates_no_exception_was_expected()
               => ExceptionMessage().Must().Contain("to not throw any exception");

            [XF] public void includes_the_original_exception_as_inner_exception()
            {
               var assertionException = Invoking(() => Invoking(() => throw new InvalidOperationException("test error"))
                                                      .Must()
                                                      .NotThrow())
                                       .Must()
                                       .Throw<AssertionFailedException>();

               assertionException.Which.InnerException.Must().Satisfy(it => it is InvalidOperationException);
               assertionException.Which.InnerException!.Message.Must().Be("test error");
            }
         }
      }
   }

   public class given_an_action_that_throws_different_exception_types : When_using_Invoking_and_NotThrow
   {
      [XF] public void NotThrow_catches_ArgumentException()
         => Invoking(() => Invoking(() => throw new ArgumentException("arg error"))
                          .Must()
                          .NotThrow())
           .Must()
           .Throw<AssertionFailedException>()
           .Which
           .Message
           .Must()
           .Contain("System.ArgumentException");

      [XF] public void NotThrow_catches_InvalidOperationException()
         => Invoking(() => Invoking(() => throw new InvalidOperationException("op error"))
                          .Must()
                          .NotThrow())
           .Must()
           .Throw<AssertionFailedException>()
           .Which
           .Message
           .Must()
           .Contain("System.InvalidOperationException");
   }

   public class given_an_action_with_return_value : When_using_Invoking_and_NotThrow
   {
      [XF] public void NotThrow_works_with_actions_that_return_values()
      {
         var value = 42;
         Invoking(() => value = 100)
            .Must()
            .NotThrow();

         value.Must().Be(100);
      }
   }
}
