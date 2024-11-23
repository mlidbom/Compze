using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.SystemCE;

///<summary>Simple utility class that calls the supplied action when the instance is async disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
class AsyncDisposableCE : IAsyncDisposable
{
   readonly Func<Task> _action;

   ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
   AsyncDisposableCE(Func<Task> action)
   {
      Assert.Argument.NotNull(action);
      _action = action;
   }

   ///<summary>Invokes the action passed to the constructor.</summary>
   public async ValueTask DisposeAsync()
   {
      await _action().NoMarshalling();
   }

   ///<summary>Constructs an object that will call <param name="action"> when disposed.</param></summary>
   public static IAsyncDisposable Create(Func<Task> action) => new AsyncDisposableCE(action);
}