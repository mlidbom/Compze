using Compze.Threading.Exceptions;

namespace Compze.Threading.Testing;

public partial interface IThreadGate : IThreadGateVisitor
{
   ///<summary>Returns a new <see cref="IThreadGate"/> with <c><see cref="IsOpen"/> == false</c>, using the supplied <paramref name="timeout"/> and <paramref name="name"/>. Auto generates a name if none is supplied.</summary>
   static IThreadGate NewClosed(WaitTimeout timeout, string? name = null) => new Implementation(timeout, name);
   ///<summary>Returns a new <see cref="IThreadGate"/> with <c><see cref="IsOpen"/> == true</c>, using the supplied <paramref name="timeout"/> and <paramref name="name"/>. Auto generates a name if none is supplied.</summary>
   static IThreadGate NewOpen(WaitTimeout timeout, string? name = null) => NewClosed(timeout, name).Open();

   ///<summary>Returns a new Closed <see cref="IThreadGate"/> using the supplied <paramref name="monitor"/>, <paramref name="timeout"/> and <paramref name="name"/>. Auto generates a name if none is supplied. Useful when <paramref name="monitor"/> needs to be shared in order to guarantee atomicity of operations involving multiple gates etc.</summary>
   internal static IThreadGate NewClosed(WaitTimeout timeout, IAwaitableMonitor monitor, string? name = null) => new Implementation(timeout, monitor, name);

   ///<summary>Sets <see cref="IsOpen"/> to true, making <see cref="IThreadGateVisitor.AwaitPassThrough"/> a non-blocking operation and releasing any threads currently blocking there.</summary>
   IThreadGate Open();

   ///<summary>Lets a single thread exit <see cref="IThreadGateVisitor.AwaitPassThrough"/> and returns the <see cref="ThreadSnapshot"/> of that thread.</summary>
   ThreadSnapshot AwaitLetOneThreadPassThrough();

   ///<summary>Sets <see cref="IsOpen"/> to false, making all callers to <see cref="IThreadGateVisitor.AwaitPassThrough"/> block.</summary>
   IThreadGate Close();

   ///<summary>Injects an action that will be called at the end of every <see cref="IThreadGateVisitor.AwaitPassThrough"/> invocation.</summary>
   IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action);

   ///<summary>Blocks until the gate is in a state which satisfies <paramref name="condition"/> then executes <paramref name="action"/>. Throws <exception cref="AwaitingConditionTimeoutException" /> if <paramref name="timeout"/> expires before <paramref name="condition"/> becomes true.</summary>
   IThreadGate ExecuteWithExclusiveLockWhen(Func<IThreadGate, bool> condition, Action action, WaitTimeout? timeout = null);

   ///<summary>Blocks until the gate is in a state which satisfies <paramref name="condition"/> or <paramref name="timeout"/> expires. Returns false if <paramref name="timeout"/> expires, else true.</summary>
   bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout = null);

   ///<summary>If false all callers of <see cref="IThreadGateVisitor.AwaitPassThrough"/> block, otherwise not.</summary>
   bool IsOpen { get; }
   ///<summary>The number of threads currently blocked at <see cref="IThreadGateVisitor.AwaitPassThrough"/>.</summary>
   int Queued { get; }
   ///<summary>The total number of threads that have called <see cref="IThreadGateVisitor.AwaitPassThrough"/> regardless of whether they are still blocked or not.</summary>
   int Requested { get; }
   ///<summary>The total number of threads that have exited <see cref="IThreadGateVisitor.AwaitPassThrough"/>.</summary>
   int Passed { get; }
   ///<summary>The timeout used in calls to <c>Await*</c> methods if the <c>timeout</c> passed is null. </summary>
   WaitTimeout WaitTimeout { get; }

   ///<summary>A <see cref="ThreadSnapshot"/> for each thread that has exited <see cref="IThreadGateVisitor.AwaitPassThrough"/>. Can be used to examine the order of threads passing through, and their state at the time, such as whether they were executing a transaction.</summary>
   IReadOnlyList<ThreadSnapshot> PassedThrough { get; }
}
