using Compze.Contracts;

namespace Compze.Threading;

///<summary>
/// A value that its factory creates the first time it is read. Every later read returns that same value, which may be null.<br/>
/// Safe to read from several threads: the factory runs once even when threads read the value for the first time<br/>
/// at the same moment. The other threads wait for it and get the value it created.
///</summary>
///<remarks>
/// Like <see cref="Lazy{T}"/> in its default thread-safety mode, with two differences.<br/>
/// If the factory throws, the exception isn't remembered: the read throws it, and the next read runs the factory again.<br/>
/// A thread waits for another thread's factory at most <see cref="LockTimeout.Default"/>, then throws a <see cref="TimeoutException"/>,<br/>
/// so a deadlock fails loud instead of hanging.<br/>
/// A factory that reads the value it is creating, directly or through another <see cref="LazyCE{TValue}"/>, makes the read<br/>
/// throw an <see cref="InvalidOperationException"/>, as <see cref="Lazy{T}"/> does, instead of running again and again until the stack overflows.
///</remarks>
///<remarks>
/// An instance is one small object, cheap enough to create in large numbers: it allocates no lock, but locks on itself,<br/>
/// and only while the value is still being created. So don't lock on an instance from outside: that code would share its lock,<br/>
/// and reading the value for the first time while holding it throws the same <see cref="InvalidOperationException"/>.<br/>
/// Once the value is created, the instance lets go of the factory, so that it, and whatever it captured, can be collected.
///</remarks>
public sealed class LazyCE<TValue>
   where TValue : class?
{
   ///<summary>The factory until the value is created, and null from then on: so the instance needs no field of its own to know whether<br/>
   /// the value is created, which it couldn't tell from the value itself when the factory created null.</summary>
   Func<TValue>? _factoryUntilTheValueIsCreated;
   TValue? _value;

   ///<summary>An instance whose value <paramref name="factory"/> creates the first time it is read.</summary>
   ///<param name="factory">
   /// Creates the value. It runs while the instance's lock is held, so it must not wait for another thread<br/>
   /// that is reading the value of this same instance.
   ///</param>
   public LazyCE(Func<TValue> factory)
   {
      Argument.NotNull(factory);
      _factoryUntilTheValueIsCreated = factory;
   }

   ///<summary>The value: created by the factory on the first read, and the same on every later read.</summary>
   public TValue Value => IsInitialized ? _value! : CreateUnlessAnotherThreadAlreadyHas();

   ///<summary>Whether a read has created the value. Never runs the factory.</summary>
   ///<remarks>Read with acquire semantics (<see cref="Volatile"/>), paired with the release in <see cref="Create"/>: a thread that sees it true<br/>
   /// also sees the value, and everything the factory wrote before returning it.</remarks>
   public bool IsInitialized => Volatile.Read(ref _factoryUntilTheValueIsCreated) is null;

   ///<summary>The value if a read has already created it, otherwise null. Never runs the factory.<br/>
   /// When the factory may create null, <see cref="IsInitialized"/> tells a value created null from one not yet created.</summary>
   public TValue? ValueIfInitialized() => IsInitialized ? _value : null;

   ///<summary>Locks this instance and, unless another thread created the value while this one waited for the lock, creates it.</summary>
   ///<remarks>
   /// The lock is held only while the value is being created, so this thread already holding it means the factory is reading the value<br/>
   /// it is creating, directly or through another <see cref="LazyCE{TValue}"/>, or that code which locked on this instance from outside<br/>
   /// is reading it. Entering the lock again would run the factory again, endlessly, until the stack overflowed and ended the process;<br/>
   /// it throws instead.
   ///</remarks>
   ///<exception cref="TimeoutException">Another thread held the lock longer than <see cref="LockTimeout.Default"/>, most likely because of a deadlock.</exception>
   ///<exception cref="InvalidOperationException">This thread already holds the lock on this instance.</exception>
   TValue CreateUnlessAnotherThreadAlreadyHas()
   {
      State.Assert(!Monitor.IsEntered(this),
                   () => "This thread already holds this LazyCE's lock, which is held only while its value is being created. "
                       + "Either its factory read the value it is creating, directly or through another LazyCE, "
                       + "or code that locked on the LazyCE from outside read its value, which LazyCE's documentation forbids.");

      //rationale: A raw Monitor instead of the Compze.Threading locking primitives, which allocate an IMonitor per instance:
      //   see dev_docs/code_standard_exceptions/raw-monitor-in-lazy-initialization.md.
#pragma warning disable CA2002 // Code outside can reach a LazyCE to lock on it too: the trade-off approved in the document above, and LazyCE's documentation tells it not to.
      if(!Monitor.TryEnter(this, LockTimeout.Default))
#pragma warning restore CA2002
         throw new TimeoutException($"Timed out after {LockTimeout.Default.Value} waiting for another thread to finish creating a lazily created value. This most likely indicates a deadlock.");

      try
      {
         return IsInitialized ? _value! : Create();
      }
      finally
      {
         Monitor.Exit(this);
      }
   }

   ///<summary>Runs the factory, under the lock, and publishes the value it creates. If the factory throws, nothing changes,<br/>
   /// and the next read runs it again.</summary>
   ///<remarks>The value is written before the factory is let go of, and that with release semantics (<see cref="Volatile"/>),<br/>
   /// so a thread that sees <see cref="IsInitialized"/> true without taking the lock also sees the value.</remarks>
   TValue Create()
   {
      var created = _factoryUntilTheValueIsCreated!();
      _value = created;
      Volatile.Write(ref _factoryUntilTheValueIsCreated, null);
      return created;
   }
}
