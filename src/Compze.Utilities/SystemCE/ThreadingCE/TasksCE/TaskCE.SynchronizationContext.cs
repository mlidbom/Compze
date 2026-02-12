using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class ConfigureAwaitCE
{
    ///<summary>
    /// Abbreviated version of <see cref="Task.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
    /// Ensures that awaits do not capture the synchronization context, thus avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
    /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
    /// named as caf to signal that this is more of a patch for a missing language feature than a traditional extension method and because we just found code with caf plain easier and faster to read.
    ///</summary>
    public static ConfiguredTaskAwaitable caf(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

    ///<summary>
    /// Abbreviated version of <see cref="Task{TResult}.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
    /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
    /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
    /// named as caf to signal that this is more of a patch for a missing language feature than a traditional extension method and because we just found code with caf plain easier and faster to read.
    ///</summary>
    public static ConfiguredTaskAwaitable<TResult> caf<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

    ///<summary>
    /// Abbreviated version of <see cref="TaskAsyncEnumerableExtensions.ConfigureAwait(IAsyncDisposable,bool)"/> with <c>continueOnCapturedContext: false</c>.
    /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
    /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
    /// named as caf to signal that this is more of a patch for a missing language feature than a traditional extension method and because we just found code with caf plain easier and faster to read.
    ///</summary>
    public static ConfiguredAsyncDisposable caf(this IAsyncDisposable @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

    ///<summary>
    /// Abbreviated version of <see cref="ValueTask.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
    /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
    /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
    /// named as caf to signal that this is more of a patch for a missing language feature than a traditional extension method and because we just found code with caf plain easier and faster to read.
    ///</summary>
    public static ConfiguredValueTaskAwaitable caf(this ValueTask @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

    ///<summary>
    /// Abbreviated version of <see cref="ValueTask.ConfigureAwait(bool)"/> with <c>continueOnCapturedContext: false</c>.
    /// Ensures that awaits do not capture the synchronization context, avoiding deadlocks and improving performance in environments with a synchronization context (e.g., UI threads).
    /// Must be applied to all awaits in library code to ensure that no deadlocks occur due to a synchronization context.
    /// named as caf to signal that this is more of a patch for a missing language feature than a traditional extension method and because we just found code with caf plain easier and faster to read.
    ///</summary>
    public static ConfiguredValueTaskAwaitable<TResult> caf<TResult>(this ValueTask<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
}
