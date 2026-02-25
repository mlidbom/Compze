using System;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Contracts;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE.Testing;

namespace Compze.Utilities.SystemCE.ThreadingCE.Testing;

public static class ThreadGateExtensions
{
   public static IThreadGate Await(this IThreadGate @this, Func<bool> condition) => @this.Await(@this.DefaultTimeout, condition);
   public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Func<bool> condition) => @this.ExecuteWithExclusiveLockWhen(timeout, condition, () => {});

   public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(() => !@this.IsOpen);

   public static IThreadGate AwaitQueueLengthEqualTo(this IThreadGate @this, int length) => @this.Await(() => @this.Queued == length);
   public static IThreadGate AwaitQueueLengthEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.Await(timeout, () => @this.Queued == length);
   public static bool TryAwaitQueueLengthEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Queued == length);

   public static IThreadGate AwaitPassedThroughCountEqualTo(this IThreadGate @this, int length) => @this.Await(() => @this.Passed == length);
   public static IThreadGate AwaitPassedThroughCountEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.Await(timeout, () => @this.Passed == length);
   public static bool TryAwaitPassedThroughCountEqualTo(this IThreadGate @this, int count, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Passed == count);

   public static IThreadGate ThrowPostPassThrough(this IThreadGate @this, Exception exception) => @this.SetPostPassThroughAction(_ => throw exception);

   public static IThreadGate FailTransactionOnPreparePostPassThrough(this IThreadGate @this, Exception exception) => @this.SetPostPassThroughAction(_ =>
   {
      ContractAssertion.State.NotNull(Transaction.Current);
      Transaction.Current.FailOnPrepare(exception);
   });

   public static Task<IThreadGate> ThrowOnNextPassThroughAsync(this IThreadGate @this, Func<ThreadSnapshot, Exception> exceptionFactory)
   {
      var currentPassthroughAction = @this.PassThroughAction;
      var currentPassedThroughCountPlusOne = @this.PassedThrough.Count + 1;
      @this.SetPassThroughAction(threadSnapshot => throw exceptionFactory(threadSnapshot));
      return @this.ExecuteWithExclusiveLockWhenAsync(TimeSpan.FromMinutes(1), () => currentPassedThroughCountPlusOne == @this.PassedThrough.Count, () => @this.SetPassThroughAction(currentPassthroughAction));
   }

   public static Task<IThreadGate> ExecuteWithExclusiveLockWhenAsync(this IThreadGate @this, TimeSpan timeout, Func<bool> condition, Action action)
      => TaskCE.Run(() => @this.ExecuteWithExclusiveLockWhen(timeout, condition, action));
}
