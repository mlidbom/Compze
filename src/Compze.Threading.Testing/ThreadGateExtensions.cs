using System;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Contracts;
using Compze.Threading.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE.Testing;

namespace Compze.Threading.Testing;

public static class ThreadGateExtensions
{
   extension(IThreadGate @this)
   {
      IThreadGate Await(Func<bool> condition) => @this.Await(@this.DefaultTimeout, condition);
      IThreadGate Await(WaitTimeout timeout, Func<bool> condition) => @this.ExecuteWithExclusiveLockWhen(timeout, condition, () => {});
      internal IThreadGate AwaitClosed() => @this.Await(() => !@this.IsOpen);
      public IThreadGate AwaitQueueLengthEqualTo(int length) => @this.Await(() => @this.Queued == length);
      public IThreadGate AwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => @this.Await(timeout, () => @this.Queued == length);
      public bool TryAwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => @this.TryAwait(timeout, () => @this.Queued == length);
      public IThreadGate AwaitPassedThroughCountEqualTo(int length) => @this.Await(() => @this.Passed == length);
      public IThreadGate AwaitPassedThroughCountEqualTo(int length, WaitTimeout timeout) => @this.Await(timeout, () => @this.Passed == length);
      public bool TryAwaitPassedThroughCountEqualTo(int count, WaitTimeout timeout) => @this.TryAwait(timeout, () => @this.Passed == count);
      public IThreadGate ThrowPostPassThrough(Exception exception) => @this.SetPostPassThroughAction(_ => throw exception);

      public IThreadGate FailTransactionOnPreparePostPassThrough(Exception exception) => @this.SetPostPassThroughAction(_ =>
      {
         Contract.State.NotNull(Transaction.Current);
         Transaction.Current.FailOnPrepare(exception);
      });

      public Task<IThreadGate> ThrowOnNextPassThroughAsync(Func<ThreadSnapshot, Exception> exceptionFactory)
      {
         var currentPassthroughAction = @this.PassThroughAction;
         var currentPassedThroughCountPlusOne = @this.PassedThrough.Count + 1;
         @this.SetPassThroughAction(threadSnapshot => throw exceptionFactory(threadSnapshot));
         return @this.ExecuteWithExclusiveLockWhenAsync(WaitTimeout.Minutes(1), () => currentPassedThroughCountPlusOne == @this.PassedThrough.Count, () => @this.SetPassThroughAction(currentPassthroughAction));
      }

      Task<IThreadGate> ExecuteWithExclusiveLockWhenAsync(WaitTimeout timeout, Func<bool> condition, Action action)
         => TaskCE.Run(() => @this.ExecuteWithExclusiveLockWhen(timeout, condition, action));
   }
}
