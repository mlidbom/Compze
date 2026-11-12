namespace Compze.Threading.Interprocess._private;

/// <summary>
/// A cross-process signaling mechanism backed by an <see cref="InterprocessChangeCounter"/>.
/// Call <see cref="Raise"/> to signal, and <see cref="TryAwait(TimeSpan, ref long, DateTime, CancellationToken)"/>, or <see cref="TryAwaitChangeFromAsync"/> in async code, to wait for a signal.
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

   ///<summary>Returns the current counter value. Pass the result to <see cref="TryAwait(TimeSpan, ref long, DateTime, CancellationToken)"/> to wait for changes relative to this point.</summary>
   public long Snapshot() => _counter.Count;

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from <paramref name="baseline"/>, treating this call as the start of the wait for <see cref="ISignalPollingPolicy"/> scheduling purposes.</summary>
   public bool TryAwait(TimeSpan timeout, ref long baseline, CancellationToken cancellationToken = default) =>
      TryAwait(timeout, ref baseline, waitStartedAt: DateTime.UtcNow, cancellationToken);

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from <paramref name="baseline"/>. Returns true if a signal was detected, false on timeout. On success, updates <paramref name="baseline"/> to the current counter value.<br/>
   /// <paramref name="waitStartedAt"/> is when the caller's logical wait began: a wait that spans several <see cref="TryAwait(TimeSpan, ref long, DateTime, CancellationToken)"/> calls
   /// keeps backing off across them instead of restarting the <see cref="ISignalPollingPolicy"/> schedule on each call.</summary>
   public bool TryAwait(TimeSpan timeout, ref long baseline, DateTime waitStartedAt, CancellationToken cancellationToken = default)
   {
      var deadline = DateTime.UtcNow + timeout;

      while(_counter.Count == baseline)
      {
         cancellationToken.ThrowIfCancellationRequested();
         if(SleepBeforeTheNextPoll(deadline, waitStartedAt) is not {} sleep)
            return false;

         // Wait on the cancellation token's handle rather than Thread.Sleep so cancellation wakes us instantly even mid-interval.
         cancellationToken.WaitHandle.WaitOne(sleep);
      }

      baseline = _counter.Count;
      return true;
   }

   ///<summary>The async counterpart of <see cref="TryAwait(TimeSpan, ref long, DateTime, CancellationToken)"/>: awaits up to <paramref name="timeout"/> for the counter<br/>
   /// to change from <paramref name="baseline"/>, polling on the same <see cref="ISignalPollingPolicy"/> schedule, but holding no thread between polls.<br/>
   /// Returns true if a signal was detected, false on timeout.</summary>
   public async Task<bool> TryAwaitChangeFromAsync(long baseline, TimeSpan timeout, DateTime waitStartedAt, CancellationToken cancellationToken)
   {
      var deadline = DateTime.UtcNow + timeout;

      while(_counter.Count == baseline)
      {
         cancellationToken.ThrowIfCancellationRequested();
         if(SleepBeforeTheNextPoll(deadline, waitStartedAt) is not {} sleep)
            return false;

         //Suppressed so that cancellation surfaces as the OperationCanceledException thrown above, as in the synchronous wait, not as Task.Delay's TaskCanceledException.
         await Task.Delay(sleep, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
      }

      return true;
   }

   ///<summary>How long to sleep before polling the counter again: what the <see cref="ISignalPollingPolicy"/> asks for, but never past <paramref name="deadline"/>.<br/>
   /// Null once <paramref name="deadline"/> has passed.</summary>
   TimeSpan? SleepBeforeTheNextPoll(DateTime deadline, DateTime waitStartedAt)
   {
      var now = DateTime.UtcNow;
      if(now >= deadline) return null;

      var pollInterval = _pollingPolicy.NextPollInterval(now - waitStartedAt);
      var untilDeadline = deadline - now;
      return pollInterval < untilDeadline ? pollInterval : untilDeadline;
   }

   public void Dispose() => _counter.Dispose();
}
