namespace Compze.Threading;

///<summary>Extension methods for <see cref="ICriticalSection"/>.</summary>
public static class MonitorCEExtensions
{
   ///<summary>
   /// Performs thread-safe lazy initialization of <paramref name="field"/> using the double-checked locking pattern.<br/>
   /// First reads <paramref name="field"/> without locking. If null, acquires the lock, re-reads, and if still null
   /// calls <paramref name="createValue"/> and atomically publishes the result into <paramref name="field"/> via
   /// <see cref="Interlocked.Exchange{T}(ref T, T)"/>.<br/>
   /// The only mutation is this single atomic reference exchange — <paramref name="createValue"/> must not perform
   /// any other shared-state mutations.
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult>(this ICriticalSection @this, ref TResult? field, Func<TResult> createValue)
      where TResult : class
   {
      var result = field;
      if(result != null) return result;

      using(@this.TakeLock())
      {
         result = field;
         if(result != null) return result;
         var newValue = createValue();
         Interlocked.Exchange(ref field!, newValue);
         return newValue;
      }
   }

   ///<summary>
   /// Performs thread-safe double-checked locking where the read operation (<paramref name="tryRead"/>) may differ
   /// from the field being exchanged.<br/>
   /// First calls <paramref name="tryRead"/> without locking. If it returns null, acquires the lock, retries
   /// <paramref name="tryRead"/>, and if still null calls <paramref name="createUpdatedFieldValue"/> and atomically
   /// publishes the result into <paramref name="field"/> via <see cref="Interlocked.Exchange{T}(ref T, T)"/>.<br/>
   /// The only mutation is this single atomic reference exchange — <paramref name="createUpdatedFieldValue"/> must
   /// produce a new object rather than mutating the existing one.<br/>
   /// Throws if <paramref name="tryRead"/> still returns null after the exchange.
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult, TField>(this ICriticalSection @this, Func<TResult?> tryRead, ref TField field, Func<TField> createUpdatedFieldValue)
      where TResult : class
      where TField : class
   {
      var result = tryRead();
      if(result != null) return result;

      using(@this.TakeLock())
      {
         result = tryRead();
         if(result != null) return result;
         Interlocked.Exchange(ref field, createUpdatedFieldValue());
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(createUpdatedFieldValue)} was called and its result was exchanged into {nameof(field)}.");
      }
   }
}
