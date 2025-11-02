using System;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_InvokingAsync_and_ThrowAsync : UniversalTestBase
{
   public class given_an_async_action_that_throws_the_expected_exception : When_using_InvokingAsync_and_ThrowAsync
   {
      readonly InvalidOperationException _actual = new InvalidOperationException("test message");

      [XF] public async Task and_allows_asserting_on_the_exception()
      {
         var caught = await InvokingAsync(async () =>
                            {
                               await Task.Yield();
                               throw _actual;
                            })
                           .Must()
                           .ThrowAsync<InvalidOperationException>();

         caught.WhichMust.Be(_actual);
      }
   }

   public class given_an_async_action_that_throws_a_different_exception : When_using_InvokingAsync_and_ThrowAsync
   {
      public class ThrowAsync_throws : given_an_async_action_that_throws_a_different_exception
      {
         [XF] public async Task when_wrong_exception_type_is_thrown()
         {
            await InvokingAsync(async () => await InvokingAsync(async () =>
                                            {
                                               await Task.Yield();
                                               throw new InvalidOperationException("wrong");
                                            })
                                           .Must()
                                           .ThrowAsync<ArgumentException>())
                 .Must()
                 .ThrowAsync<AssertionFailedException>();
         }

         public class and_the_exception_message : ThrowAsync_throws
         {
            async Task<string> ExceptionMessage() => (await InvokingAsync(async () => await InvokingAsync(async () =>
                                                                                      {
                                                                                         await Task.Yield();
                                                                                         throw new InvalidOperationException("wrong");
                                                                                      })
                                                                                     .Must()
                                                                                     .ThrowAsync<ArgumentException>())
                                                            .Must()
                                                            .ThrowAsync<AssertionFailedException>())
                                                     .Which
                                                     .Message;

            [XF] public async Task is_the_full_formatted_message()
            {
               var message = await ExceptionMessage();
               message.Must().Be($$"""
                                   Expected invoking the expression
                                   --------------------------------------------------
                                   async () =>
                                   {
                                      await Task.Yield();
                                      throw new InvalidOperationException("wrong");
                                   } 
                                   --------------------------------------------------
                                   to throw ArgumentException but instead a System.InvalidOperationException was thrown
                                   """);
            }
         }
      }
   }

   public class given_an_async_action_that_does_not_throw : When_using_InvokingAsync_and_ThrowAsync
   {
      public class ThrowAsync_throws : given_an_async_action_that_does_not_throw
      {
         [XF] public async Task when_no_exception_is_thrown()
         {
            await InvokingAsync(async () => await InvokingAsync(async () =>
                                            {
                                               await Task.Yield();
                                               // do nothing
                                            })
                                           .Must()
                                           .ThrowAsync<InvalidOperationException>())
                 .Must()
                 .ThrowAsync<AssertionFailedException>();
         }

         public class and_the_exception_message : ThrowAsync_throws
         {
            async Task<string> ExceptionMessage() => (await InvokingAsync(async () => await InvokingAsync(async () =>
                                                                                      {
                                                                                         await Task.Yield();
                                                                                         // do nothing
                                                                                      })
                                                                                     .Must()
                                                                                     .ThrowAsync<InvalidOperationException>())
                                                            .Must()
                                                            .ThrowAsync<AssertionFailedException>())
                                                     .Which
                                                     .Message;

            [XF] public async Task is_the_full_formatted_message()
            {
               var message = await ExceptionMessage();
               message.Must().Be("""
                                 Expected invoking the expression
                                 --------------------------------------------------
                                 async () =>
                                 {
                                    await Task.Yield();
                                    // do nothing
                                 } 
                                 --------------------------------------------------
                                 to throw InvalidOperationException but no exception was thrown
                                 """);
            }
         }
      }
   }

   public class given_an_async_action_that_throws_a_derived_exception : When_using_InvokingAsync_and_ThrowAsync
   {
      public class ThrowAsync_catches_the_derived_exception : given_an_async_action_that_throws_a_derived_exception
      {
         // ReSharper disable once NotResolvedInText
         readonly ArgumentNullException _actual = new("argument");

         [XF] public async Task when_expecting_base_exception_type()
         {
            var caught = await InvokingAsync(async () =>
                               {
                                  await Task.Yield();
                                  throw _actual;
                               })
                              .Must()
                              .ThrowAsync<ArgumentException>();

            caught.That.Must().Be(_actual);
         }
      }
   }

   public class given_async_code_that_validates_exceptions : When_using_InvokingAsync_and_ThrowAsync
   {
      public class InvokingAsync_can_be_used_recursively : given_async_code_that_validates_exceptions
      {
         [XF] public async Task to_test_exception_throwing_validation_code()
         {
            // Test that validation code correctly catches the expected exception
            var caughtException = await InvokingAsync(async () =>
                                        {
                                           await Task.Yield();
                                           throw new InvalidOperationException("inner");
                                        })
                                       .Must()
                                       .ThrowAsync<InvalidOperationException>();

            // Verify the caught exception has the expected properties
            caughtException.Which.Message.Must().Contain("inner");
         }
      }
   }

   public class given_an_async_action_with_complex_error_details : When_using_InvokingAsync_and_ThrowAsync
   {
      record TestObject(string Name, int Value);

      public class ThrowAsync_enables_detailed_assertions : given_an_async_action_with_complex_error_details
      {
         [XF] public async Task on_exception_with_complex_data()
         {
            var testData = new TestObject("test", 42);
            var exception = await InvokingAsync(async () =>
                                  {
                                     await Task.Yield();
                                     throw new InvalidOperationException($"Failed with {testData}");
                                  })
                               .Must()
                               .ThrowAsync<InvalidOperationException>();

            exception.Which.Message.Must().Be("Failed with TestObject { Name = test, Value = 42 }");
         }
      }
   }

   public class given_an_async_action_that_completes_synchronously : When_using_InvokingAsync_and_ThrowAsync
   {
      readonly InvalidOperationException _actual = new InvalidOperationException("sync exception");

      [XF] public async Task ThrowAsync_catches_synchronously_thrown_exceptions()
      {
         var caught = await InvokingAsync(() =>
                            {
                               throw _actual;
#pragma warning disable CS0162 // Unreachable code detected
                               return Task.CompletedTask;
#pragma warning restore CS0162
                            })
                           .Must()
                           .ThrowAsync<InvalidOperationException>();

         caught.WhichMust.Be(_actual);
      }
   }

   public class given_an_async_action_with_aggregate_exception : When_using_InvokingAsync_and_ThrowAsync
   {
      [XF] public async Task ThrowAsync_can_catch_aggregate_exceptions()
      {
         var innerException = new InvalidOperationException("inner");
         var aggregateException = new AggregateException(innerException);

         var caught = await InvokingAsync(async () =>
                            {
                               await Task.Yield();
                               throw aggregateException;
                            })
                           .Must()
                           .ThrowAsync<AggregateException>();

         caught.Which.InnerExceptions.Must().HaveCount(1);
         caught.Which.InnerExceptions[0].Must().Be(innerException);
      }
   }
}
