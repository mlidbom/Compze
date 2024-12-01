using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   ///<summary>
   /// Abbreviated version of <see cref="Task.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, thus avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
   ///</summary>
   public static ConfiguredTaskAwaitable CaF(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="Task{TResult}.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
   ///</summary>
   public static ConfiguredTaskAwaitable<TResult> CaF<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="TaskAsyncEnumerableExtensions.ConfigureAwait(IAsyncDisposable,bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
   ///</summary>
   public static ConfiguredAsyncDisposable CaF(this IAsyncDisposable @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="ValueTask{TResult}.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
   ///</summary>
   public static ConfiguredValueTaskAwaitable<TResult> CaF<TResult>(this ValueTask<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="ValueTask.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
   ///</summary>
   public static ConfiguredValueTaskAwaitable CaF(this ValueTask @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
}
