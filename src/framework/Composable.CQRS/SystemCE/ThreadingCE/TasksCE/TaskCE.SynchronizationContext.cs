using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   ///<summary>
   /// Abbreviated version of <see cref="Task.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, improving performance and avoiding deadlocks in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code for consistent behavior and performance.
   ///</summary>
   public static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="Task{TResult}.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, improving performance and avoiding deadlocks in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code for consistent behavior and performance.
   ///</summary>
   public static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="TaskAsyncEnumerableExtensions.ConfigureAwait(IAsyncDisposable,bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, improving performance and avoiding deadlocks in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code for consistent behavior and performance.
   ///</summary>
   public static ConfiguredAsyncDisposable NoMarshalling(this IAsyncDisposable @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="ValueTask{TResult}.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, improving performance and avoiding deadlocks in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code for consistent behavior and performance.
   ///</summary>
   public static ConfiguredValueTaskAwaitable<TResult> NoMarshalling<TResult>(this ValueTask<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

   ///<summary>
   /// Abbreviated version of <see cref="ValueTask.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
   /// Ensures that awaits do not capture the synchronization context, improving performance and avoiding deadlocks in environments with a synchronization context (e.g., UI threads).
   /// Must be applied to all awaits in library code for consistent behavior and performance.
   ///</summary>
   public static ConfiguredValueTaskAwaitable NoMarshalling(this ValueTask @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
}
