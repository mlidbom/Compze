namespace Compze.Threading.Testing;

public interface IThreadGateVisitor
{
   Unit AwaitPassThrough();
}

public partial interface IThreadGate : IThreadGateVisitor
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
   IThreadGate ExecuteWithExclusiveLockWhen(Func<IThreadGate, bool> condition, Action action, WaitTimeout? timeout = null);

   bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout = null);

   bool IsOpen { get; }
   int Queued { get; }
   int Requested { get; }
   int Passed { get; }
   WaitTimeout WaitTimeout { get; }

   IReadOnlyList<ThreadSnapshot> PassedThrough { get; }
}

///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public partial interface IGatedCodeSection
{
   static IGatedCodeSection NewClosed(WaitTimeout timeout, string name) => new GatedCodeSection(timeout, name);
   static IGatedCodeSection NewOpen(WaitTimeout timeout, string name) => NewClosed(timeout, name).Open();

   IThreadGate EntranceGate { get; }
   IThreadGate ExitGate { get; }
   IDisposable Enter();
}
