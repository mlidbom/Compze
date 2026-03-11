using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Threading.Exceptions;

namespace Compze.Threading.Testing;

public partial interface IThreadGate
{
   private class Implementation : IThreadGate
   {
      public WaitTimeout WaitTimeout { get; }

      public bool IsOpen { get; private set; }

      public int Queued => _lock.Read(() => _queuedThreads.Count);
      public int Passed => _lock.Read(() => _passedThreads.Count);
      public int Requested => _lock.Read(() => _requestsThreads.Count);

      public IReadOnlyList<ThreadSnapshot> PassedThrough => _lock.Read(() => _passedThreads.ToList());

      public IThreadGate Open()
      {
         using var _ = LogMethodEntryExit(nameof(Open));
         _lock.Update(() =>
         {
            IsOpen = true;
            _lockOnNextPass = false;
         });
         return this;
      }

      public ThreadSnapshot AwaitLetOneThreadPassThrough()
      {
         using var _ = LogMethodEntryExit(nameof(AwaitLetOneThreadPassThrough));
         var passedBefore = 0;
         _lock.Update(() =>
         {
            State.Assert(!IsOpen);
            passedBefore = _passedThreads.Count;
            IsOpen = true;
            _lockOnNextPass = true;
         });
         ((IThreadGate)this).AwaitClosed();
         return _lock.Read(() => _passedThreads[passedBefore]);
      }

      public bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout) => _lock.TryAwait(() => condition(this), timeout);

      public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => this._mutate(_ => _lock.Update(() => _postPassThroughAction = action));

      public IThreadGate ExecuteWithExclusiveLockWhen(Func<IThreadGate, bool> condition, Action action, WaitTimeout? timeout = null)
      {
         try
         {
            using(_lock.TakeUpdateLockWhen(() => condition(this), timeout))
            {
               action();
            }
         }
         catch(AwaitingConditionTimeoutException parentException)
         {
            throw new AwaitingConditionTimeoutException(parentException,
                                                        $"""

                                                         Current state of gate: 
                                                         {this}
                                                         """);
         }

         return this;
      }

      public IThreadGate Close()
      {
         using var _ = LogMethodEntryExit(nameof(Close));
         _lock.Update(() => IsOpen = false);
         return this;
      }

      public Unit AwaitPassThrough()
      {
         using var _ = LogMethodEntryExit(nameof(AwaitPassThrough));

         var currentThread = new ThreadSnapshot();

         _lock.Update(() =>
         {
            _requestsThreads.Add(currentThread);
            _queuedThreads.AddLast(currentThread);
         });

         using(_lock.TakeUpdateLockWhen(() => IsOpen))
         {
            if(_lockOnNextPass)
            {
               _lockOnNextPass = false;
               IsOpen = false;
            }

            _queuedThreads.Remove(currentThread);
            _passedThreads.Add(currentThread);
            _postPassThroughAction.Invoke(currentThread);
         }
         return unit;
      }

      internal Implementation(WaitTimeout waitTimeout, string? name = null)
      {
         Name = name ?? Guid.NewGuid().ToString();
         _lock = IAwaitableMonitor.New(LockTimeout.Default, waitTimeout);
         WaitTimeout = waitTimeout;
      }

      public override string ToString() => $"{nameof(Implementation)} {{ {nameof(Name)}: {Name} {nameof(IsOpen)} : {IsOpen}, {nameof(Queued)}: {Queued}, {nameof(Passed)}: {Passed}, {nameof(Requested)}: {Requested} }}";

      IDisposable LogMethodEntryExit(string method) => _lock.Update(() =>
      {
         Log($"Thread:{Thread.CurrentThread.GetHashCode()} Entering gate method:{Name}.{method}");
         return new Disposable(() => _lock.Update(() => Log($"Thread:{Thread.CurrentThread.GetHashCode()} Exiting gate method:{Name}.{method}")));

         void Log(string tevent) => this.Log().Info($"{tevent} {this}");
      });

      string Name { get; }
      readonly IAwaitableMonitor _lock;
      bool _lockOnNextPass;
      Action<ThreadSnapshot> _postPassThroughAction = _ => {};
      readonly List<ThreadSnapshot> _requestsThreads = [];
      readonly LinkedList<ThreadSnapshot> _queuedThreads = [];
      readonly List<ThreadSnapshot> _passedThreads = [];
   }
}
