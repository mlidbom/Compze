// ReSharper disable InconsistentNaming
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using static Compze.Must.MustActions;

namespace Compze.Internals.SystemCE.Tests.ThreadingCE;

/// <summary>
/// TaskCE's WaitUnwrappingException and ResultUnwrappingException rethrow a task's failure as the exception itself when it<br/>
/// has one, and as an <see cref="AggregateException"/> holding them all when it has several. That is what sets them apart<br/>
/// from <c>GetAwaiter().GetResult()</c>, which throws only the first of several, and loses the rest.
/// </summary>
public class When_waiting_synchronously_through_TaskCE_for_a_task_that_failed
{
   static async Task Fail(string message)
   {
      await Task.Yield();
      throw new InvalidOperationException(message);
   }

   static async Task<int> FailWithAResult(string message)
   {
      await Task.Yield();
      throw new InvalidOperationException(message);
   }

   public class with_one_exception : When_waiting_synchronously_through_TaskCE_for_a_task_that_failed
   {
      [XF] public void WaitUnwrappingException_throws_that_exception() =>
         Invoking(() => Fail("the only one").WaitUnwrappingException())
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("the only one");

      [XF] public void ResultUnwrappingException_throws_that_exception() =>
         Invoking(() => FailWithAResult("the only one").ResultUnwrappingException())
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("the only one");
   }

   public class with_two_exceptions : When_waiting_synchronously_through_TaskCE_for_a_task_that_failed
   {
      [XF] public void WaitUnwrappingException_throws_an_AggregateException_holding_both() =>
         Invoking(() => Task.WhenAll(Fail("the first"), Fail("the second")).WaitUnwrappingException())
           .Must().Throw<AggregateException>()
           .Which.InnerExceptions.Select(it => it.Message).Must().SequenceEqual(["the first", "the second"]);

      [XF] public void ResultUnwrappingException_throws_an_AggregateException_holding_both() =>
         Invoking(() => Task.WhenAll(FailWithAResult("the first"), FailWithAResult("the second")).ResultUnwrappingException())
           .Must().Throw<AggregateException>()
           .Which.InnerExceptions.Select(it => it.Message).Must().SequenceEqual(["the first", "the second"]);
   }
}
