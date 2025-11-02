using System;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_InvokingAsync_and_NotThrowAsync : UniversalTestBase
{
   public class given_an_async_action_that_does_not_throw : When_using_InvokingAsync_and_NotThrowAsync
   {
      [XF] public async Task NotThrowAsync_succeeds()
      {
         await InvokingAsync(async () =>
               {
                  await Task.Yield();
                  // do nothing
               })
              .Must()
              .NotThrowAsync();
      }

      [XF] public async Task NotThrowAsync_returns_the_Must_for_chaining()
      {
         var result = await InvokingAsync(async () =>
                            {
                               await Task.Yield();
                               // do nothing
                            })
                           .Must()
                           .NotThrowAsync();

         result.Must().Satisfy(it => it is not null);
      }
   }

   public class given_an_async_action_that_throws_an_exception : When_using_InvokingAsync_and_NotThrowAsync
   {
      public class NotThrowAsync_throws : given_an_async_action_that_throws_an_exception
      {
         [XF] public async Task when_any_exception_is_thrown()
         {
            await InvokingAsync(async () => await InvokingAsync(async () =>
                                            {
                                               await Task.Yield();
                                               throw new InvalidOperationException("error");
                                            })
                                           .Must()
                                           .NotThrowAsync())
                 .Must()
                 .ThrowAsync<AssertionFailedException>();
         }

         public class and_the_exception_message : NotThrowAsync_throws
         {
            async Task<string> ExceptionMessage() => (await InvokingAsync(async () => await InvokingAsync(async () =>
                                                                                      {
                                                                                         await Task.Yield();
                                                                                         throw new InvalidOperationException("test error");
                                                                                      })
                                                                                     .Must()
                                                                                     .NotThrowAsync())
                                                            .Must()
                                                            .ThrowAsync<AssertionFailedException>())
                                                     .Which
                                                     .Message;

            [XF] public async Task contains_the_expression()
            {
               var message = await ExceptionMessage();
               message.Must().Contain("async () =>");
            }

            [XF] public async Task contains_the_exception_type()
            {
               var message = await ExceptionMessage();
               message.Must().Contain("System.InvalidOperationException");
            }

            [XF] public async Task contains_the_exception_message()
            {
               var message = await ExceptionMessage();
               message.Must().Contain("test error");
            }

            [XF] public async Task indicates_no_exception_was_expected()
            {
               var message = await ExceptionMessage();
               message.Must().Contain("to not throw any exception");
            }

            [XF] public async Task includes_the_original_exception_as_inner_exception()
            {
               var assertionException = await InvokingAsync(async () => await InvokingAsync(async () =>
                                                            {
                                                               await Task.Yield();
                                                               throw new InvalidOperationException("test error");
                                                            })
                                                           .Must()
                                                           .NotThrowAsync())
                                             .Must()
                                             .ThrowAsync<AssertionFailedException>();

               assertionException.Which.InnerException.Must().Satisfy(it => it is InvalidOperationException);
               assertionException.Which.InnerException!.Message.Must().Be("test error");
            }
         }
      }
   }

   public class given_an_async_action_that_throws_different_exception_types : When_using_InvokingAsync_and_NotThrowAsync
   {
      [XF] public async Task NotThrowAsync_catches_ArgumentException()
      {
         var exception = await InvokingAsync(async () => await InvokingAsync(async () =>
                                                         {
                                                            await Task.Yield();
                                                            throw new ArgumentException("arg error");
                                                         })
                                                        .Must()
                                                        .NotThrowAsync())
                              .Must()
                              .ThrowAsync<AssertionFailedException>();

         exception.Which.Message.Must().Contain("System.ArgumentException");
      }

      [XF] public async Task NotThrowAsync_catches_InvalidOperationException()
      {
         var exception = await InvokingAsync(async () => await InvokingAsync(async () =>
                                                         {
                                                            await Task.Yield();
                                                            throw new InvalidOperationException("op error");
                                                         })
                                                        .Must()
                                                        .NotThrowAsync())
                              .Must()
                              .ThrowAsync<AssertionFailedException>();

         exception.Which.Message.Must().Contain("System.InvalidOperationException");
      }
   }

   public class given_an_async_action_that_completes_synchronously : When_using_InvokingAsync_and_NotThrowAsync
   {
      [XF] public async Task NotThrowAsync_works_with_synchronously_completing_tasks()
      {
         await InvokingAsync(() => Task.CompletedTask)
              .Must()
              .NotThrowAsync();
      }

      [XF] public async Task NotThrowAsync_catches_synchronously_thrown_exceptions()
      {
         await InvokingAsync(async () => await InvokingAsync(() =>
                                         {
                                            throw new InvalidOperationException("sync error");
#pragma warning disable CS0162 // Unreachable code detected
                                            return Task.CompletedTask;
#pragma warning restore CS0162
                                         })
                                        .Must()
                                        .NotThrowAsync())
              .Must()
              .ThrowAsync<AssertionFailedException>();
      }
   }

   public class given_an_async_action_with_return_value : When_using_InvokingAsync_and_NotThrowAsync
   {
      [XF] public async Task NotThrowAsync_works_with_async_methods_that_set_values()
      {
         var value = 42;
         await InvokingAsync(async () =>
               {
                  await Task.Yield();
                  value = 100;
               })
              .Must()
              .NotThrowAsync();

         value.Must().Be(100);
      }
   }
}
