namespace Compze.Threading.Interprocess;

/// <summary>
/// A cross-process signaling mechanism backed by an <see cref="InterprocessChangeCounter"/>.
/// Call <see cref="Raise"/> to signal, and <see cref="TryAwait"/> to wait for a signal.
/// <see cref="TryAwait"/> polls the underlying counter at ~1ms intervals — a nanosecond memory read.
/// </summary>
class InterprocessSignal : IDisposable
{
   static readonly PollingInterval CounterPollingInterval = PollingInterval.Milliseconds(1);

   readonly InterprocessChangeCounter _counter;
   long _baseline;

   public InterprocessSignal(string name, DirectoryInfo directory)
   {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be null, empty, or whitespace", nameof(name));
      if(!directory.Exists) throw new DirectoryNotFoundException($"Directory does not exist: {directory.FullName}");

      var backingFile = new FileInfo(Path.Combine(directory.FullName, name.Replace('\\', '_') + ".signal"));
      _counter = new InterprocessChangeCounter(backingFile);
      _baseline = _counter.Count;
   }

   public void Raise() => _counter.Increment();

   ///<summary>Records the current counter value. Subsequent <see cref="TryAwait"/> calls wait for changes relative to this snapshot.</summary>
   public void Snapshot() => _baseline = _counter.Count;

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from the last snapshot. Returns true if a signal was detected, false on timeout. On success, automatically takes a new snapshot.</summary>
   public bool TryAwait(TimeSpan timeout)
   {
      var deadline = DateTime.UtcNow + timeout;

      while(_counter.Count == _baseline)
      {
         if(DateTime.UtcNow >= deadline)
            return false;

         Thread.Sleep(CounterPollingInterval);
      }

      _baseline = _counter.Count;
      return true;
   }

   public void Dispose() => _counter.Dispose();
}
