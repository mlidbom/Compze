using Compze.Internals.SystemCE._private;
using Compze.Threading;

namespace Compze.Internals.SystemCE;

///<summary>
/// A value that <paramref name="factory"/> creates the first time it is read. Every later read returns that same value.<br/>
/// Safe to read from several threads: <paramref name="factory"/> runs once even when threads read the value for the first time<br/>
/// at the same moment. The other threads wait for it and get the value it created.
///</summary>
///<remarks>
/// Like <see cref="Lazy{T}"/> in its default thread-safety mode, with two differences.<br/>
/// If <paramref name="factory"/> throws, the exception isn't remembered: the read throws it, and the next read runs <paramref name="factory"/> again.<br/>
/// A thread waits for another thread's <paramref name="factory"/> at most <see cref="LockTimeout.Default"/>, then throws a <see cref="TimeoutException"/>,<br/>
/// so a deadlock fails loud instead of hanging.<br/>
/// A <paramref name="factory"/> that reads the value it is creating, directly or through another <see cref="LazyCE{TValue}"/>, makes the read<br/>
/// throw an <see cref="InvalidOperationException"/>, as <see cref="Lazy{T}"/> does, instead of running again and again until the stack overflows.
///</remarks>
///<remarks>
/// An instance is one small object, cheap enough to create in large numbers: it allocates no lock, but locks on itself,<br/>
/// and only while the value is still being created. So don't lock on an instance from outside: that code would share its lock,<br/>
/// and reading the value for the first time while holding it throws the same <see cref="InvalidOperationException"/>.
///</remarks>
///<param name="factory">
/// Creates the value. It runs while the instance's lock is held, so it must not wait for another thread<br/>
/// that is reading the value of this same instance.
///</param>
public sealed class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly Func<TValue> _factory = factory;
   TValue? _value;

   ///<summary>The value: created by the factory on the first read, and the same instance on every later read.</summary>
   public TValue Value => DoubleCheckedLockingOnAnExistingObject.GetOrCreate(ref _value, _factory, lockingOn: this);

   ///<summary>The value if a read has already created it, otherwise null. Never runs the factory.</summary>
   public TValue? ValueIfInitialized() => Volatile.Read(ref _value);
}
