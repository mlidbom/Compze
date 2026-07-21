// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

///<summary>The <c>Func&lt;Task&gt;</c>/<c>Func&lt;Task&lt;T&gt;&gt;</c> <c>Must()</c> overloads: the sugar that lets an async lambda<br/>
/// start a throw-assertion chain directly, without spelling out <c>InvokingAsync</c>.</summary>
public class When_calling_Must_on_an_async_lambda : UniversalTestBase
{
   public class returning_a_plain_Task : When_calling_Must_on_an_async_lambda
   {
      [XF] public async Task ThrowAsync_catches_the_exception_the_awaited_action_throws() =>
         await ((Func<Task>)(() => Task.FromException(new InvalidOperationException("boom"))))
            .Must().ThrowAsync<InvalidOperationException>();
   }

   public class returning_a_Task_with_a_result : When_calling_Must_on_an_async_lambda
   {
      [XF] public async Task ThrowAsync_catches_the_exception_the_awaited_function_throws() =>
         await ((Func<Task<int>>)(() => Task.FromException<int>(new InvalidOperationException("boom"))))
            .Must().ThrowAsync<InvalidOperationException>();
   }
}
