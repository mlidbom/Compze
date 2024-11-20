﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Testing.Threading;

class ThreadGate : IThreadGate
{
   public static IThreadGate CreateClosedWithTimeout(TimeSpan timeout) => new ThreadGate(timeout);
   public static IThreadGate CreateOpenWithTimeout(TimeSpan timeout) => new ThreadGate(timeout).Open();

   public TimeSpan DefaultTimeout { get; }

   public bool IsOpen { get; private set; }

   public long Queued => _monitor.Read(() => _queuedThreads.Count);
   public long Passed => _monitor.Read(() => _passedThreads.Count);
   public long Requested => _monitor.Read(() => _requestsThreads.Count);

   public IReadOnlyList<ThreadSnapshot> RequestedThreads => _monitor.Read(() => _requestsThreads.ToList());
   public IReadOnlyList<ThreadSnapshot> QueuedThreads => _monitor.Read(() => _queuedThreads.ToList());
   public IReadOnlyList<ThreadSnapshot> PassedThrough => _monitor.Read(() => _passedThreads.ToList());
   public Action<ThreadSnapshot> PassThroughAction => _monitor.Read(() => _passThroughAction);

   public IThreadGate Open()
   {
      _monitor.Update(() =>
      {
         IsOpen = true;
         _lockOnNextPass = false;
      });
      return this;
   }

   public IThreadGate AwaitLetOneThreadPassThrough()
   {
      _monitor.Update(() =>
      {
         Contract.Assert.That(!IsOpen, "Gate must be closed to call this method.");
         IsOpen = true;
         _lockOnNextPass = true;
      });
      return this.AwaitClosed();
   }

   public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _monitor.TryAwait(timeout, condition);

   public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _monitor.Update(() => _postPassThroughAction = action));
   public IThreadGate SetPrePassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _monitor.Update(() => _prePassThroughAction = action));
   public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _monitor.Update(() => _passThroughAction = action));

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
      _monitor.Update(() => IsOpen = false);
      return this;
   }

   public void AwaitPassThrough()
   {
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
   }

   ThreadGate(TimeSpan defaultTimeout)
   {
      _monitor = MonitorCE.WithTimeout(defaultTimeout);
      DefaultTimeout = defaultTimeout;
   }

   public override string ToString() => $@"{nameof(IsOpen)} : {IsOpen},
{nameof(Queued)}: {Queued},
{nameof(Passed)}: {Passed},
{nameof(Requested)}: {Requested},
";

   readonly MonitorCE _monitor;
   bool _lockOnNextPass;
   Action<ThreadSnapshot> _passThroughAction = _ => {};
   Action<ThreadSnapshot> _prePassThroughAction = _ => {};
   Action<ThreadSnapshot> _postPassThroughAction = _ => {};
   readonly List<ThreadSnapshot> _requestsThreads = [];
   readonly LinkedList<ThreadSnapshot> _queuedThreads = [];
   readonly List<ThreadSnapshot> _passedThreads = [];
}