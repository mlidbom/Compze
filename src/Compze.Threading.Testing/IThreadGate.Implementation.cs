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

      public IThreadGate Open() => _monitor.Update(() => LogMethodEntryExit(() =>
      {
         IsOpen = true;
         _lockOnNextPass = false;
      })).__(this);

      public IThreadGate Close() => _monitor.Update(() => LogMethodEntryExit(() =>
      {
         IsOpen = false;
      })).__(this);

      public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => _monitor.Update(() => LogMethodEntryExit(() =>
      {
         _postPassThroughAction = action;
      })).__(this);

      public ThreadSnapshot AwaitLetOneThreadPassThrough() => _monitor.Update(() => LogMethodEntryExit(() =>
      {
         var passedBefore = 0;
         _monitor.Update(() =>
         {
            State.Assert(!IsOpen);
            passedBefore = _passedThreads.Count;
            IsOpen = true;
            _lockOnNextPass = true;
         });

         return _monitor.UpdateWhen(() => !IsOpen, () => _passedThreads[passedBefore]);
      }));

      public bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!) =>
         _monitor.Read(() => LogMethodEntryExit(() => _monitor.TryAwait(() => condition(this), waitTimeout: timeout),
                                                message: $"condition: '{conditionExpression}'",
                                                logResult: true));

      public TResult ExecuteWithExclusiveLockWhen<TResult>(Func<IThreadGate, bool> condition, Func<TResult> func, WaitTimeout? timeout = null, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!)
         => _monitor.Update(() => LogMethodEntryExit(() =>
                                                     {
                                                        try
                                                        {
                                                           using(_monitor.TakeUpdateLockWhen(() => condition(this), waitTimeout: timeout))
                                                           {
                                                              return func();
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
                                                     },
                                                     message: $"condition: '{conditionExpression}'"));

      public Unit AwaitPassThrough() => _monitor.Update(() => LogMethodEntryExit(() =>
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
      }));

      internal Implementation(WaitTimeout waitTimeout, string? name = null) : this(waitTimeout, IAwaitableMonitor.New(LockTimeout.Default, waitTimeout), name) {}

      internal Implementation(WaitTimeout waitTimeout, IAwaitableMonitor sharedMonitor, string? name = null)
      {
         Name = name ?? Guid.NewGuid().ToString();
         _monitor = sharedMonitor;
         WaitTimeout = waitTimeout;
      }

      public override string ToString() => $"{{ {nameof(Name)}: {Name} {nameof(IsOpen)} : {IsOpen}, {nameof(Queued)}: {Queued}, {nameof(Passed)}: {Passed}, {nameof(Requested)}: {Requested} }}";

      void LogMethodEntry(string message = "", [CallerMemberName] string method = null!)
      {
         var messageSection = string.IsNullOrEmpty(message) ? "" : $" {message}";
         _monitor.Read(() => this.Log().Info($"Thread:{Thread.CurrentThread.GetHashCode()} Entering gate method:{Name}.{method}{messageSection} {this}"));
      }

      void LogMethodExit(string message = "", string exitInfo = "", [CallerMemberName] string method = null!)
      {
         var messageSection = string.IsNullOrEmpty(message) ? "" : $" {message}";
         var exitSection = string.IsNullOrEmpty(exitInfo) ? "" : $" {exitInfo}";
         _monitor.Read(() => this.Log().Info($"Thread:{Thread.CurrentThread.GetHashCode()} Exiting gate method:{Name}.{method}{messageSection} {this}{exitSection}"));
      }

      Unit LogMethodEntryExit(Action action, string message = "", bool logResult = false, [CallerMemberName] string method = null!) => LogMethodEntryExit(action.ToFunc(), message, logResult, method);

      TResult LogMethodEntryExit<TResult>(Func<TResult> body, string message = "", bool logResult = false, [CallerMemberName] string method = null!)
      {
         LogMethodEntry(message, method);
         try
         {
            var result = body();
            LogMethodExit(message, logResult ? $"Result: {result}" : "", method);
            return result;
         }
         catch(Exception exception)
         {
            LogMethodExit(message, $"Exception: {exception.GetType().Name}", method);
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
