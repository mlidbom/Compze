using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

namespace Compze.Internals.Logging;

public static class LevelLoggerExtensions
{
   public static IDisposable LogMethodEntryExit(this ILevelLogger @this, [CallerMemberName] string caller = "") =>
#pragma warning disable CA2000// We are passing this out of the method...
      @this.Log("Entering", caller)
           ._then(new Disposable(() => @this.Log("Exiting", caller)));
#pragma warning restore CA2000

   public static IDisposable LogMethodExecutionTime(this ILevelLogger @this, [CallerMemberName] string caller = "")
      => Stopwatch.StartNew()._(it => new Disposable(() => @this.Log($"Executed in {it.Elapsed}", caller)));

   public static IDisposable LogEntryExit(this ILevelLogger @this, string message = "", [CallerMemberName] string caller = "") =>
#pragma warning disable CA2000// We are passing this disposable out of the method
      @this.Log($"Entering {message}", caller)
           ._then(new Disposable(() => @this.Log($"Exiting {message}", caller)));
#pragma warning restore CA2000
}
