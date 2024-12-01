using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Contracts;

public class AssertAndRun(Action assertion)
{
   readonly Action _assertion = assertion;

   internal TResult Do<TResult>(Func<TResult> action)
   {
      _assertion();
      return action();
   }

   internal void Do(Action action)
   {
      _assertion();
      action();
   }

   internal async Task<TResult> DoAsync<TResult>(Func<Task<TResult>> action)
   {
      _assertion();
      return await action().CaF();
   }

   internal async Task DoAsync(Func<Task> action)
   {
      _assertion();
      await action().CaF();
   }

   internal void Assert() => _assertion();
}