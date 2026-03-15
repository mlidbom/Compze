using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Threading.Exceptions;
using System.Runtime.CompilerServices;

namespace Compze.Threading.Testing;

public partial interface IThreadGate
{
   private class Implementation : IThreadGate
   {
      public WaitTimeout WaitTimeout { get; }

      public bool IsOpen { get; private set; }

      public int Queued => _monitor.Read(() => _queuedThreads.Count);
      public int Passed => _monitor.Read(() => _passedThreads.Count);
      public int Requested => _monitor.Read(() => _requestsThreads.Count);

      public IReadOnlyList<ThreadSnapshot> PassedThrough => _monitor.Read(() => _passedThreads.ToList());

      public IThreadGate Open() => LogMethodEntryExit(() =>
      {
         _monitor.Update(() =>
         {
            IsOpen = true;
            _lockOnNextPass = false;
         });
         return this;
      });

      public ThreadSnapshot AwaitLetOneThreadPassThrough() => LogMethodEntryExit(() =>
      {
         var passedBefore = 0;
         _monitor.Update(() =>
         {
            State.Assert(!IsOpen);
            passedBefore = _passedThreads.Count;
            IsOpen = true;
            _lockOnNextPass = true;
         });
         ((IThreadGate)this).AwaitClosed();
         return _monitor.Read(() => _passedThreads[passedBefore]);
      });

      public bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!) =>
         LogMethodEntryExit(() =>
         {
            var returnValue = _monitor.TryAwait(() => condition(this), timeout);
            return returnValue;
         }, message: $"condition: '{conditionExpression}'");

      public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => this._mutate(_ => _monitor.Update(() => _postPassThroughAction = action));

      public IThreadGate ExecuteWithExclusiveLockWhen(Func<IThreadGate, bool> condition, Action action, WaitTimeout? timeout = null, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!) =>
         LogMethodEntryExit(() =>
         {
            try
            {
               using(_monitor.TakeUpdateLockWhen(() => condition(this), timeout))
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
         }, message: $"condition: '{conditionExpression}'");

      public IThreadGate Close() => LogMethodEntryExit(() =>
      {
         _monitor.Update(() => IsOpen = false);
         return this;
      });

      public Unit AwaitPassThrough() => LogMethodEntryExit(() =>
      {
         var currentThread = new ThreadSnapshot();

         _monitor.Update(() =>
         {
            _requestsThreads.Add(currentThread);
            _queuedThreads.AddLast(currentThread);
         });

         using(_monitor.TakeUpdateLockWhen(() => IsOpen))
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
      });

      internal Implementation(WaitTimeout waitTimeout, string? name = null) : this(waitTimeout, IAwaitableMonitor.New(LockTimeout.Default, waitTimeout), name) {}

      internal Implementation(WaitTimeout waitTimeout, IAwaitableMonitor sharedMonitor, string? name = null)
      {
         Name = name ?? Guid.NewGuid().ToString();
         _monitor = sharedMonitor;
         WaitTimeout = waitTimeout;
      }

      public override string ToString() => $"{{ {nameof(Name)}: {Name} {nameof(IsOpen)} : {IsOpen}, {nameof(Queued)}: {Queued}, {nameof(Passed)}: {Passed}, {nameof(Requested)}: {Requested} }}";

      void LogMethodEntry(string method) => _monitor.Read(() => this.Log().Info($"Thread:{Thread.CurrentThread.GetHashCode()} Entering gate method:{Name}.{method} {this}"));
      void LogMethodExit(string method, string message = "") => _monitor.Read(() => this.Log().Info($"Thread:{Thread.CurrentThread.GetHashCode()} Exiting gate method:{Name}.{method} {this} {message}"));

      TResult LogMethodEntryExit<TResult>(Func<TResult> body, string message = "", [CallerMemberName] string method = null!)
      {
         var logContext = string.IsNullOrEmpty(message) ? method : $"{method}: {message}";
         LogMethodEntry(logContext);
         try
         {
            var result = body();
            LogMethodExit(logContext);
            return result;
         }
         catch(Exception exception)
         {
            LogMethodExit(logContext, $"Exception: {exception.GetType().Name}");
            throw;
         }
      }

      string Name { get; }
      readonly IAwaitableMonitor _monitor;
      bool _lockOnNextPass;
      Action<ThreadSnapshot> _postPassThroughAction = _ => {};
      readonly List<ThreadSnapshot> _requestsThreads = [];
      readonly LinkedList<ThreadSnapshot> _queuedThreads = [];
      readonly List<ThreadSnapshot> _passedThreads = [];
   }
}
