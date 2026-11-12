using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
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
         var passedBefore = _monitor.Update(OpenForOnePass);
         return _monitor.UpdateWhen(() => !IsOpen, () => _passedThreads[passedBefore]);
      }));

      public Task<ThreadSnapshot> AwaitLetOneThreadPassThroughAsync() => LogMethodEntryExitAsync(async () =>
      {
         var passedBefore = _monitor.Update(OpenForOnePass);
         return await _monitor.ReadWhenAsync(() => !IsOpen, () => _passedThreads[passedBefore]).caf();
      });

      public bool TryAwait(Func<IThreadGate, bool> condition, WaitTimeout? timeout, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!) =>
         _monitor.Read(() => LogMethodEntryExit(() => _monitor.TryAwait(() => condition(this), waitTimeout: timeout),
                                                message: $"condition: '{conditionExpression}'",
                                                logResult: true));

      public Task<bool> TryAwaitAsync(Func<IThreadGate, bool> condition, WaitTimeout? timeout, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!) =>
         LogMethodEntryExitAsync(() => _monitor.TryAwaitAsync(() => condition(this), waitTimeout: timeout),
                                 message: $"condition: '{conditionExpression}'",
                                 logResult: true);

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
                                                           throw TimedOutAwaiting(conditionExpression, parentException);
                                                        }
                                                     },
                                                     message: $"condition: '{conditionExpression}'"));

      public Task<TResult> ExecuteWithExclusiveLockWhenAsync<TResult>(Func<IThreadGate, bool> condition, Func<TResult> func, WaitTimeout? timeout = null, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null!)
         => LogMethodEntryExitAsync(async () =>
                                    {
                                       try
                                       {
                                          return await _monitor.UpdateWhenAsync(() => condition(this), func, waitTimeout: timeout).caf();
                                       }
                                       catch(AwaitingConditionTimeoutException parentException)
                                       {
                                          throw TimedOutAwaiting(conditionExpression, parentException);
                                       }
                                    },
                                    message: $"condition: '{conditionExpression}'");

      public Unit AwaitPassThrough() => _monitor.Update(() => LogMethodEntryExit(() =>
      {
         var passingThread = new ThreadSnapshot();
         _monitor.Update(() => Enqueue(passingThread));
         using(_monitor.TakeUpdateLockWhen(() => IsOpen)) PassThrough(passingThread);
      }));

      public async Task AwaitPassThroughAsync()
      {
         //Taken before the first await, so it records the caller's state, such as its ambient transaction.
         var passingThread = new ThreadSnapshot();
         await LogMethodEntryExitAsync(async () =>
         {
            _monitor.Update(() => Enqueue(passingThread));
            await _monitor.UpdateWhenAsync(() => IsOpen, () => PassThrough(passingThread)).caf();
            return unit;
         }).caf();
      }

      ///<summary>Opens the gate for exactly one pass: the passing thread closes it again. Returns how many threads had passed before, the index in<br/>
      /// <see cref="_passedThreads"/> the one let through will have. Called holding the lock.</summary>
      int OpenForOnePass()
      {
         State.Assert(!IsOpen);
         IsOpen = true;
         _lockOnNextPass = true;
         return _passedThreads.Count;
      }

      ///<summary>Registers <paramref name="passingThread"/> as requesting to pass and queued at the gate. Called holding the lock.</summary>
      void Enqueue(ThreadSnapshot passingThread)
      {
         _requestsThreads.Add(passingThread);
         _queuedThreads.AddLast(passingThread);
      }

      ///<summary>Lets <paramref name="passingThread"/>, queued at the open gate, pass through it, closing the gate behind it if it was opened for one pass,<br/>
      /// and runs the post-pass-through action. Called holding the update lock, with the gate open.</summary>
      void PassThrough(ThreadSnapshot passingThread)
      {
         if(_lockOnNextPass)
         {
            _lockOnNextPass = false;
            IsOpen = false;
         }

         _queuedThreads.Remove(passingThread);
         _passedThreads.Add(passingThread);
         _postPassThroughAction.Invoke(passingThread);
      }

      AwaitingConditionTimeoutException TimedOutAwaiting(string? conditionExpression, AwaitingConditionTimeoutException parentException) =>
         new(parentException,
             $"""

              Condition: '{conditionExpression}'
              Current state of gate:
              {this}
              Thread pool: {ThreadPoolCE.DescribeState()}
              """);

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

      async Task<TResult> LogMethodEntryExitAsync<TResult>(Func<Task<TResult>> body, string message = "", bool logResult = false, [CallerMemberName] string method = null!)
      {
         LogMethodEntry(message, method);
         try
         {
            var result = await body().caf();
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
