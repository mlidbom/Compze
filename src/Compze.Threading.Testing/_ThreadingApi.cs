namespace Compze.Threading.Testing;

public interface IThreadGateVisitor
{
   unit AwaitPassThrough();
}

public interface IThreadGate : IThreadGateVisitor
{
   ///<summary>Opens the gate and lets all threads through.</summary>
   IThreadGate Open();

   ///<summary>Lets a single thread pass.</summary>
   IThreadGate AwaitLetOneThreadPassThrough();

   ///<summary>Blocks all threads from passing.</summary>
   IThreadGate Close();

   IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action);

   ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
   IThreadGate ExecuteWithExclusiveLockWhen(WaitTimeout timeout, Func<bool> condition, Action action);

   bool TryAwait(WaitTimeout timeout, Func<bool> condition);

   bool IsOpen { get; }
   int Queued { get; }
   int Requested { get; }
   int Passed { get; }
   WaitTimeout DefaultTimeout { get; }

   IReadOnlyList<ThreadSnapshot> PassedThrough { get; }
}

///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public interface IGatedCodeSection
{
   IThreadGate EntranceGate { get; }
   IThreadGate ExitGate { get; }
   IDisposable Enter();
}
