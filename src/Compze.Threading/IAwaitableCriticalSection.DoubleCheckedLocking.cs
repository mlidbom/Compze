namespace Compze.Threading;

///<summary>Extension methods for <see cref="IAwaitableCriticalSection"/>.</summary>
public static class AwaitableCriticalSectionDoubleCheckedLocking
{
   extension(IAwaitableCriticalSection @this)
   {
      ///<summary>
      /// Thread-safe lazy initialization of <paramref name="field"/> by double-checked locking: returns <paramref name="field"/> if it is set,<br/>
      /// and otherwise sets it to what <paramref name="createFieldValue"/> returns, and returns that.<br/>
      /// Reading <paramref name="field"/> once it is set takes no lock.
      ///</summary>
      ///<remarks>
      /// <paramref name="createFieldValue"/> runs while the update lock is held, after <paramref name="field"/> is read again under the lock,<br/>
      /// so it runs once however many threads call this at the same moment, and threads waiting for a condition on this critical section<br/>
      /// evaluate it again once the value is set. Since it runs under the lock, it must not wait for another thread that takes this<br/>
      /// critical section. If it throws, <paramref name="field"/> stays unset and the next call runs it again, so any state it changed<br/>
      /// before throwing is changed again.
      ///</remarks>
      ///<remarks>
      /// A thread that gets the value without taking the lock also sees everything <paramref name="createFieldValue"/> wrote before<br/>
      /// returning it, wherever it wrote it: the lock-free read of <paramref name="field"/> is an acquire, paired with the release<br/>
      /// that publishes the value (<see cref="Volatile"/>).
      ///</remarks>
      public TResult DoubleCheckedLocking<TResult>(ref TResult? field, Func<TResult> createFieldValue)
         where TResult : class
      {
         var result = Volatile.Read(ref field);
         if(result != null) return result;

         using(@this.TakeReadLock())
         {
            result = field;
            if(result != null) return result;

            using(@this.TakeUpdateLock())
            {
               var newValue = createFieldValue();
               Volatile.Write(ref field, newValue);
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
      public TResult DoubleCheckedLocking<TResult, TField>(Func<TResult?> tryRead, ref TField field, Func<TField> createUpdatedFieldValue)
         where TResult : class
         where TField : class
      {
         //refactor: tryRead reads field itself, with an ordinary read, so a thread that sees the value may still read stale state
         //   that createUpdatedFieldValue wrote elsewhere: that is what the WARNING in the summary is about. The fix needs an
         //   acquiring read before tryRead uses the field: tryRead would take the field's value as a parameter, read here with
         //   Volatile.Read, as the single-field overload does.
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
}
