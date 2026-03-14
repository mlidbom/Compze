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

   public InterprocessSignal(string name, DirectoryInfo directory)
   {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be null, empty, or whitespace", nameof(name));
      if(!directory.Exists) throw new DirectoryNotFoundException($"Directory does not exist: {directory.FullName}");

      var backingFile = new FileInfo(Path.Combine(directory.FullName, name.Replace('\\', '_') + ".signal"));
      _counter = new InterprocessChangeCounter(backingFile);
   }

   public void Raise() => _counter.Increment();

   ///<summary>Returns the current counter value. Pass the result to <see cref="TryAwait"/> to wait for changes relative to this point.</summary>
   public long Snapshot() => _counter.Count;

   ///<summary>Waits up to <paramref name="timeout"/> for the counter to change from <paramref name="baseline"/>. Returns true if a signal was detected, false on timeout. On success, updates <paramref name="baseline"/> to the current counter value.</summary>
   public bool TryAwait(TimeSpan timeout, ref long baseline)
   {
      var deadline = DateTime.UtcNow + timeout;

      while(_counter.Count == baseline)
      {
         if(DateTime.UtcNow >= deadline)
            return false;

         Thread.Sleep(CounterPollingInterval);
      }

      baseline = _counter.Count;
      return true;
   }

   public void Dispose() => _counter.Dispose();
}
