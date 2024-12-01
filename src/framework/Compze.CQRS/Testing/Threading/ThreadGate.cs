using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;
using Compze.Functional;
using Compze.Logging;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Testing.Threading;

class ThreadGate : IThreadGate
{
   public static IThreadGate CreateClosedWithTimeout(TimeSpan timeout, string? name = null) => new ThreadGate(timeout, name);
   public static IThreadGate CreateOpenWithTimeout(TimeSpan timeout, string? name = null) => new ThreadGate(timeout, name).Open();

   public TimeSpan DefaultTimeout { get; }

   public bool IsOpen { get; private set; }

   public long Queued => _monitor.Read(() => _queuedThreads.Count);
   public long Passed => _monitor.Read(() => _passedThreads.Count);
   public long Requested => _monitor.Read(() => _requestsThreads.Count);

   public IReadOnlyList<ThreadSnapshot> RequestedThreads => _monitor.Read(() => _requestsThreads.ToList());
   public IReadOnlyList<ThreadSnapshot> QueuedThreads => _monitor.Read(() => _queuedThreads.ToList());
   public IReadOnlyList<ThreadSnapshot> PassedThrough => _monitor.Read(() => _passedThreads.ToList());
   public Unit Enablelogging(bool enable = true) => Unit.From(_enableLogging = enable);
   public Action<ThreadSnapshot> PassThroughAction => _monitor.Read(() => _passThroughAction);

   public IThreadGate Open()
   {
      using var _ = LogMethodEntryExit(nameof(Open));
      _monitor.Update(() =>
      {
         IsOpen = true;
         _lockOnNextPass = false;
      });
      return this;
   }

   public IThreadGate AwaitLetOneThreadPassThrough()
   {
      using var _ = LogMethodEntryExit(nameof(AwaitLetOneThreadPassThrough));
      _monitor.Update(() =>
      {
         Contract.Assert.That(!IsOpen, "Gate must be closed to call this method.");
         IsOpen = true;
         _lockOnNextPass = true;
      });
      return this.AwaitClosed();
   }

   public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _monitor.TryAwait(timeout, condition);

   public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => this.mutate(_ => _monitor.Update(() => _postPassThroughAction = action));
   public IThreadGate SetPrePassThroughAction(Action<ThreadSnapshot> action) => this.mutate(_ => _monitor.Update(() => _prePassThroughAction = action));
   public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => this.mutate(_ => _monitor.Update(() => _passThroughAction = action));

   public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action action)
   {
      try
      {
         using(_monitor.EnterUpdateLockWhen(timeout, condition))
         {
            action();
         }
      }
      catch(AwaitingConditionTimeoutException parentException)
      {
         throw new AwaitingConditionTimeoutException(parentException,
                                                     $@"
Current state of gate: 
{this}");
      }

      return this;
   }

   public IThreadGate Close()
   {
      using var _ = LogMethodEntryExit(nameof(Close));
      _monitor.Update(() => IsOpen = false);
      return this;
   }

   public Unit AwaitPassThrough()
   {
      using var _ = LogMethodEntryExit(nameof(AwaitPassThrough));

      var currentThread = new ThreadSnapshot();

      _monitor.Update(() =>
      {
         _requestsThreads.Add(currentThread);
         _queuedThreads.AddLast(currentThread);
      });

      using(_monitor.EnterUpdateLockWhen(() => IsOpen))
      {
         if(_lockOnNextPass)
         {
            _lockOnNextPass = false;
            IsOpen = false;
         }

         _queuedThreads.Remove(currentThread);
         _passedThreads.Add(currentThread);
         _prePassThroughAction.Invoke(currentThread);
         _passThroughAction.Invoke(currentThread);
         _postPassThroughAction.Invoke(currentThread);
      }

      return Unit.Instance;
   }

   ThreadGate(TimeSpan defaultTimeout, string? name = null)
   {
      Name = name ?? Guid.NewGuid().ToString();
      _monitor = MonitorCE.WithTimeout(defaultTimeout);
      DefaultTimeout = defaultTimeout;
   }

   public override string ToString() => $"{nameof(ThreadGate)} {{ {nameof(Name)}: {Name} {nameof(IsOpen)} : {IsOpen}, {nameof(Queued)}: {Queued}, {nameof(Passed)}: {Passed}, {nameof(Requested)}: {Requested} }}";

   static readonly List<string> GlobalLog = [];
   readonly List<string> _log = [];

   public IReadOnlyList<string> GetGlobalLog() => new List<string>(GlobalLog);
   public IReadOnlyList<string> GetLog() => new List<string>(_log);

   IDisposable LogMethodEntryExit(string method) => _monitor.Update(() =>
   {
      Log($"Entering {method}");
      return DisposableCE.Create(() => _monitor.Update(() => Log($"Exiting  {method}")));

      void Log(string @event)
      {
         if(!_enableLogging) return;

         var message = $"{@event} {this}";
         this.Log().Info(message);
         _log.Add(message);
         GlobalLogMonitor.Update(() => GlobalLog.Add(message));
      }
   });

   string Name { get; }
   readonly MonitorCE _monitor;
   static readonly MonitorCE GlobalLogMonitor = MonitorCE.WithTimeout(1.Seconds());
   bool _lockOnNextPass;
   bool _enableLogging = false;
   Action<ThreadSnapshot> _passThroughAction = _ => {};
   Action<ThreadSnapshot> _prePassThroughAction = _ => {};
   Action<ThreadSnapshot> _postPassThroughAction = _ => {};
   readonly List<ThreadSnapshot> _requestsThreads = [];
   readonly LinkedList<ThreadSnapshot> _queuedThreads = [];
   readonly List<ThreadSnapshot> _passedThreads = [];
}
