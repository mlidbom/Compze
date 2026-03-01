using System;
using System.Collections.Generic;
using Compze.Underscore;

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

   IThreadGate SetPrePassThroughAction(Action<ThreadSnapshot> action);
   IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action);
   IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action);

   ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
   IThreadGate ExecuteWithExclusiveLockWhen(WaitTimeout timeout, Func<bool> condition, Action action);

   bool TryAwait(WaitTimeout timeout, Func<bool> condition);

   Action<ThreadSnapshot> PassThroughAction { get; }

   bool IsOpen { get; }
   int Queued { get; }
   int Requested { get; }
   int Passed { get; }
   WaitTimeout DefaultTimeout { get; }

   IReadOnlyList<ThreadSnapshot> RequestedThreads { get; }
   IReadOnlyList<ThreadSnapshot> QueuedThreads { get; }
   IReadOnlyList<ThreadSnapshot> PassedThrough { get; }
   unit EnableLogging(bool enable = true);
}

///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public interface IGatedCodeSection
{
   IThreadGate EntranceGate { get; }
   IThreadGate ExitGate { get; }
   IDisposable Enter();
}
