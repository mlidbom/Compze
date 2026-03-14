namespace Compze.Threading;

///<summary>Extension methods for <see cref="IAwaitableCriticalSection"/>.</summary>
public static class IAwaitableCriticalSectionDoubleCheckedLocking
{
   ///<summary>
   /// <para>
   /// WARNING! <paramref name="createFieldValue"/> must not perform any visible state modifications or all thread safety guarantees are lost.<br/>
   /// </para>
   /// 
   /// Performs thread-safe lazy initialization of <paramref name="field"/> using the double-checked locking pattern.<br/>
   /// First reads <paramref name="field"/> without locking. If null, acquires the lock, re-reads, and if still null
   /// calls <paramref name="createFieldValue"/> and atomically updates <paramref name="field"/> via
   /// <see cref="Interlocked.Exchange{T}(ref T, T)"/> and notifies all waiting threads about the update.<br/>
   /// 
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult>(this IAwaitableCriticalSection @this, ref TResult? field, Func<TResult> createFieldValue)
      where TResult : class
   {
      var result = field;
      if(result != null) return result;

      using(@this.TakeReadLock())
      {
         result = field;
         if(result != null) return result;

         using(@this.TakeUpdateLock())
         {
            var newValue = createFieldValue();
            Interlocked.Exchange(ref field!, newValue);
            return newValue;
         }
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
   /// replaces <paramref name="field"/> via <see cref="Interlocked.Exchange{T}(ref T, T)"/> and notifies all waiting threads about the update.<br/>
   /// 
   /// Throws if <paramref name="tryRead"/> still returns null after the exchange.
   ///</summary>
   public static TResult DoubleCheckedLocking<TResult, TField>(this IAwaitableCriticalSection @this, Func<TResult?> tryRead, ref TField field, Func<TField> createUpdatedFieldValue)
      where TResult : class
      where TField : class
   {
      var result = tryRead();
      if(result != null) return result;

      using(@this.TakeReadLock())
      {
         result = tryRead();
         if(result != null) return result;

         using(@this.TakeUpdateLock())
         {
            Interlocked.Exchange(ref field, createUpdatedFieldValue());
            return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(createUpdatedFieldValue)} was called and its result was exchanged into {nameof(field)}.");
         }
      }
   }
}
