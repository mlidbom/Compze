using Compze.Threading._internal;

namespace Compze.Threading.Interprocess._private;

/// <summary>
/// A cross-process signaling mechanism backed by an <see cref="InterprocessChangeCounter"/>.
/// Call <see cref="Raise"/> to signal, and <see cref="TryAwait(TimeSpan, ref long, StartedWait, CancellationToken)"/>, or <see cref="TryAwaitChangeFromAsync"/> in async code, to wait for a signal.
/// </summary>
/// <remarks>
/// Waiting polls the counter — each poll is a cheap memory read, but each poll also wakes the CPU, and frequent wakeups prevent it
/// from reaching its deep low-power idle states. The <see cref="ISignalPollingPolicy"/> passed at construction decides the poll
/// schedule, trading signal-detection latency against power draw.
/// </remarks>
class InterprocessSignal : IDisposable
{
   readonly InterprocessChangeCounter _counter;
   readonly ISignalPollingPolicy _pollingPolicy;

   public InterprocessSignal(string name, DirectoryInfo directory, ISignalPollingPolicy? pollingPolicy = null)
   {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be null, empty, or whitespace", nameof(name));
      if(!directory.Exists) throw new DirectoryNotFoundException($"Directory does not exist: {directory.FullName}");

      var backingFile = new FileInfo(Path.Combine(directory.FullName, name.Replace('\\', '_') + ".signal"));
      _counter = new InterprocessChangeCounter(backingFile);
      _pollingPolicy = pollingPolicy ?? ISignalPollingPolicy.Default;
   }

   public void Raise() => _counter.Increment();

   ///<summary>Returns the current counter value. Pass the result to <see cref="TryAwait(TimeSpan, ref long, StartedWait, CancellationToken)"/> to wait for changes relative to this point.</summary>
   public long Snapshot() => _counter.Count;

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from <paramref name="baseline"/>, treating this call as the whole wait for<br/>
   /// <see cref="ISignalPollingPolicy"/> scheduling purposes.</summary>
   public bool TryAwait(TimeSpan timeout, ref long baseline, CancellationToken cancellationToken = default) =>
      TryAwait(timeout, ref baseline, StartedWait.Begin(new WaitTimeout(timeout)), cancellationToken);

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from <paramref name="baseline"/>. Returns true if a signal was detected, false on timeout. On success, updates <paramref name="baseline"/> to the current counter value.<br/>
   /// The call is a step of the caller's longer <paramref name="wait"/>: a wait that spans several <see cref="TryAwait(TimeSpan, ref long, StartedWait, CancellationToken)"/> calls
   /// keeps backing off across them instead of restarting the <see cref="ISignalPollingPolicy"/> schedule on each call.</summary>
   public bool TryAwait(TimeSpan timeout, ref long baseline, StartedWait wait, CancellationToken cancellationToken = default)
   {
      var step = StartedWait.Begin(new WaitTimeout(timeout));

      while(_counter.Count == baseline)
      {
         cancellationToken.ThrowIfCancellationRequested();
         if(PauseBeforeTheNextPoll(step, wait) is not {} pause)
            return false;

         // Wait on the cancellation token's handle rather than Thread.Sleep so cancellation wakes us instantly even mid-interval.
         cancellationToken.WaitHandle.WaitOne(pause);
      }

      baseline = _counter.Count;
      return true;
   }

   ///<summary>The async counterpart of <see cref="TryAwait(TimeSpan, ref long, StartedWait, CancellationToken)"/>: awaits up to <paramref name="timeout"/> for the counter<br/>
   /// to change from <paramref name="baseline"/>, a step of the caller's longer <paramref name="wait"/>, polling on the same <see cref="ISignalPollingPolicy"/> schedule,<br/>
   /// but holding no thread between polls. Returns true if a signal was detected, false on timeout.</summary>
   public async Task<bool> TryAwaitChangeFromAsync(long baseline, TimeSpan timeout, StartedWait wait, CancellationToken cancellationToken)
   {
      var step = StartedWait.Begin(new WaitTimeout(timeout));

      while(_counter.Count == baseline)
      {
         cancellationToken.ThrowIfCancellationRequested();
         if(PauseBeforeTheNextPoll(step, wait) is not {} pause)
            return false;

         //Suppressed so that cancellation surfaces as the OperationCanceledException thrown above, as in the synchronous wait, not as Task.Delay's TaskCanceledException.
         await Task.Delay(pause, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
      }

      return true;
   }

   ///<summary>How long to pause before polling the counter again: what the <see cref="ISignalPollingPolicy"/> asks for, given how long the whole<br/>
   /// <paramref name="wait"/> has waited, but never past the end of <paramref name="step"/>. Null once <paramref name="step"/> has expired.</summary>
   TimeSpan? PauseBeforeTheNextPoll(StartedWait step, StartedWait wait) =>
      step.HasExpired ? null : step.TimeRemainingButNoMoreThan(_pollingPolicy.NextPollInterval(wait.Elapsed));

   public void Dispose() => _counter.Dispose();
}
