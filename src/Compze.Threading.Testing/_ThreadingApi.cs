using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE.Testing;

namespace Compze.Threading.Testing;

public interface IThreadGateVisitor
{
   Unit AwaitPassThrough();
}

public interface IThreadGate : IThreadGateVisitor
{
   static IThreadGate NewClosed(WaitTimeout timeout, string? name = null) => ThreadGate.NewClosed(timeout, name);
   static IThreadGate NewOpen(WaitTimeout timeout, string? name = null) => ThreadGate.NewOpen(timeout, name);

   ///<summary>Opens the gate and lets all threads through.</summary>
   IThreadGate Open();

   ///<summary>Lets a single thread pass.</summary>
   IThreadGate AwaitLetOneThreadPassThrough();

   ///<summary>Blocks all threads from passing.</summary>
   IThreadGate Close();

   IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action);

   ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
   IThreadGate ExecuteWithExclusiveLockWhen(Func<bool> condition, Action action, WaitTimeout? timeout = null);

   bool TryAwait(Func<bool> condition, WaitTimeout? timeout = null);

   bool IsOpen { get; }
   int Queued { get; }
   int Requested { get; }
   int Passed { get; }
   WaitTimeout WaitTimeout { get; }

   IReadOnlyList<ThreadSnapshot> PassedThrough { get; }

   IThreadGate Await(Func<bool> condition) => Await(WaitTimeout, condition);
   IThreadGate Await(WaitTimeout? timeout, Func<bool> condition) => ExecuteWithExclusiveLockWhen(condition, () => {}, timeout);

   IThreadGate AwaitClosed() => Await(() => !IsOpen);
   IThreadGate AwaitQueueLengthEqualTo(int length) => Await(() => Queued == length);
   IThreadGate AwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => Await(timeout, () => Queued == length);
   bool TryAwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => TryAwait(() => Queued == length, timeout);
   IThreadGate AwaitPassedThroughCountEqualTo(int length) => Await(() => Passed == length);
   IThreadGate AwaitPassedThroughCountEqualTo(int length, WaitTimeout? timeout) => Await(timeout, () => Passed == length);
   bool TryAwaitPassedThroughCountEqualTo(int count, WaitTimeout? timeout = null) => TryAwait(() => Passed == count, timeout);
   IThreadGate ThrowPostPassThrough(Exception exception) => SetPostPassThroughAction(_ => throw exception);

   IThreadGate FailTransactionOnPreparePostPassThrough(Exception exception) => SetPostPassThroughAction(_ =>
   {
      State.NotNull(Transaction.Current);
      Transaction.Current.FailOnPrepare(exception);
   });
}

///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public interface IGatedCodeSection
{
   static IGatedCodeSection NewClosed(WaitTimeout timeout, string name) => GatedCodeSection.NewClosed(timeout, name);

   IThreadGate EntranceGate { get; }
   IThreadGate ExitGate { get; }
   IDisposable Enter();

   IGatedCodeSection Open()
   {
      EntranceGate.Open();
      ExitGate.Open();
      return this;
   }

   IGatedCodeSection LetOneThreadEnterAndReachExit()
   {
      State.Assert(EntranceGate.Passed == ExitGate.Passed, () => $"{nameof(IGatedCodeSection)} must be empty when calling this method");
      EntranceGate.AwaitLetOneThreadPassThrough();
      ExitGate.AwaitQueueLengthEqualTo(1);
      return this;
   }

   IGatedCodeSection LetOneThreadPass()
   {
      LetOneThreadEnterAndReachExit();
      ExitGate.AwaitLetOneThreadPassThrough();
      return this;
   }

   void Execute(Action action)
   {
      using(Enter())
      {
         action();
      }
   }
}
