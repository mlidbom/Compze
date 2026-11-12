using System.Runtime.CompilerServices;
using Compze.Contracts;

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
      /// that publishes the value (<see cref="Volatile"/>).<br/>
      /// A <paramref name="createFieldValue"/> that reads the field it is creating, directly or not, runs again and again, since the lock lets<br/>
      /// the thread holding it enter again. When the stack is nearly used up, the call throws an <see cref="InsufficientExecutionStackException"/><br/>
      /// instead of overflowing the stack, which would end the process. The lock can't tell that apart from creating another field<br/>
      /// guarded by the same critical section, which is legitimate.
      ///</remarks>
      public TResult DoubleCheckedLocking<TResult>(ref TResult? field, Func<TResult> createFieldValue)
         where TResult : class
      {
         var result = Volatile.Read(ref field);
         if(result != null) return result;

         ThrowInsteadOfOverflowingTheStackIfCreatingTheValueRecurses();
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
      /// Thread-safe double-checked locking for a <paramref name="field"/> holding many values, such as an immutable dictionary used as<br/>
      /// a cache: returns what <paramref name="tryRead"/> finds in <paramref name="field"/>, and otherwise replaces <paramref name="field"/> with<br/>
      /// what <paramref name="createUpdatedFieldValue"/> makes of it, and returns what <paramref name="tryRead"/> finds in that.<br/>
      /// When <paramref name="tryRead"/> finds the value, no lock is taken.
      ///</summary>
      ///<remarks>
      /// <paramref name="createUpdatedFieldValue"/> is given the field's current value and returns the updated one. It runs while the update lock<br/>
      /// is held, after <paramref name="tryRead"/> found nothing in the field read again under the lock, so for a missing value it runs once<br/>
      /// however many threads call this at the same moment, and threads waiting for a condition on this critical section evaluate it again<br/>
      /// once the field is updated. Since it runs under the lock, it must not wait for another thread that takes this critical section.<br/>
      /// If it throws, <paramref name="field"/> stays as it was and the next call runs it again.
      ///</remarks>
      ///<remarks>
      /// <paramref name="tryRead"/> is given the field's value read with acquire semantics, paired with the release that publishes an updated<br/>
      /// value (<see cref="Volatile"/>), so a thread that finds the value without taking the lock also sees everything<br/>
      /// <paramref name="createUpdatedFieldValue"/> wrote before returning it, wherever it wrote it.<br/>
      /// A <paramref name="createUpdatedFieldValue"/> that reads the value it is creating, directly or not, runs again and again until the stack<br/>
      /// is nearly used up, and the call then throws an <see cref="InsufficientExecutionStackException"/> instead of overflowing the stack.
      ///</remarks>
      ///<exception cref="ArgumentException"><paramref name="tryRead"/> finds nothing in what <paramref name="createUpdatedFieldValue"/> returned.</exception>
      public TResult DoubleCheckedLocking<TResult, TField>(Func<TField, TResult?> tryRead, ref TField field, Func<TField, TField> createUpdatedFieldValue)
         where TResult : class
         where TField : class
      {
         var result = tryRead(Volatile.Read(ref field));
         if(result != null) return result;

         ThrowInsteadOfOverflowingTheStackIfCreatingTheValueRecurses();
         using(@this.TakeReadLock())
         {
            var currentFieldValue = field;
            result = tryRead(currentFieldValue);
            if(result != null) return result;

            using(@this.TakeUpdateLock())
            {
               var updatedFieldValue = createUpdatedFieldValue(currentFieldValue);
               Volatile.Write(ref field, updatedFieldValue);
               var resultInTheUpdatedField = tryRead(updatedFieldValue);
               Argument.Assert(resultInTheUpdatedField != null, () => $"{nameof(tryRead)} found nothing in what {nameof(createUpdatedFieldValue)} returned.");
               return resultInTheUpdatedField;
            }
         }
      }
   }

   static void ThrowInsteadOfOverflowingTheStackIfCreatingTheValueRecurses() => RuntimeHelpers.EnsureSufficientExecutionStack();
}
