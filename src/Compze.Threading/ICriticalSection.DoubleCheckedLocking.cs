namespace Compze.Threading;

///<summary>Extension methods for <see cref="ICriticalSection"/>.</summary>
public static class CriticalSectionDoubleCheckedLocking
{
   ///<summary>
   /// <para>
   /// WARNING! <paramref name="createFieldValue"/> must not perform any visible state modifications or all thread safety guarantees are lost.<br/>
   /// </para>
   /// 
   /// Performs thread-safe lazy initialization of <paramref name="field"/> using the double-checked locking pattern.<br/>
   /// First reads <paramref name="field"/> without locking. If null, acquires the lock, re-reads, and if still null
   /// calls <paramref name="createFieldValue"/> and atomically updates <paramref name="field"/> via
   /// <see cref="Interlocked.Exchange{T}(ref T, T)"/>.<br/>
   /// 
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult>(this ICriticalSection @this, ref TResult? field, Func<TResult> createFieldValue)
      where TResult : class
   {
      //refactor: This ordinary read of field doesn't keep later reads from being done before it, so a thread that sees the value
      //   may still read stale state that createFieldValue wrote elsewhere: that is what the WARNING in the summary is about.
      //   Volatile.Read here (acquire), paired with Volatile.Write instead of Interlocked.Exchange below (release), makes
      //   everything createFieldValue wrote visible to a thread that sees the value, and the WARNING can go.
      //   DoubleCheckedLockingOnAnExistingObject in Compze.Internals.SystemCE does it this way.
      var result = field;
      if(result != null) return result;

      using(@this.TakeLock())
      {
         result = field;
         if(result != null) return result;
         var newValue = createFieldValue();
         Interlocked.Exchange(ref field!, newValue);
         return newValue;
      }
   }

   ///<summary>
   /// <para>
   /// WARNING! <paramref name="createUpdatedFieldValue"/> must not perform any visible state modifications or all thread safety guarantees are lost.<br/>
   /// </para>
   /// 
   /// Performs thread-safe double-checked locking where the read operation is more complex than just reading a nullable field. <br/>
   /// First calls <paramref name="tryRead"/> without locking. If it returns null, acquires the lock, retries
   /// <paramref name="tryRead"/>, and if still null calls <paramref name="createUpdatedFieldValue"/> and atomically
   /// replaces <paramref name="field"/> via <see cref="Interlocked.Exchange{T}(ref T, T)"/>.<br/>
   /// 
   /// Throws if <paramref name="tryRead"/> still returns null after the exchange.
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult, TField>(this ICriticalSection @this, Func<TResult?> tryRead, ref TField field, Func<TField> createUpdatedFieldValue)
      where TResult : class
      where TField : class
   {
      //refactor: tryRead reads field itself, with an ordinary read, so a thread that sees the value may still read stale state
      //   that createUpdatedFieldValue wrote elsewhere: that is what the WARNING in the summary is about. The fix needs an
      //   acquiring read before tryRead uses the field: tryRead would take the field's value as a parameter, read here with
      //   Volatile.Read. See the other overload.
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
