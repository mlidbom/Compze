using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

namespace Compze.Utilities.Logging;

public static class LevelLoggerExtensions
{
   public static IDisposable LogMethodEntryExit(this ILevelLogger @this, [CallerMemberName] string message = "") =>
#pragma warning disable CA2000// We are passing this out of the method...
      @this.Log($"Entering {message}")
           ._Then(new Disposable(() => @this.Log($"Exiting {message}")));
#pragma warning restore CA2000

   public static IDisposable LogMethodExecutionTime(this ILevelLogger @this, [CallerMemberName] string message = "")
      => Stopwatch.StartNew()._(it => new Disposable(() => @this.Log($"Executed {message} in {it.Elapsed}")));

   public static IDisposable LogEntryExit(this ILevelLogger @this, string message = "") =>
#pragma warning disable CA2000// We are passing this disposable out of the method
      @this.Log($"Entering {message}")
           ._Then(new Disposable(() => @this.Log($"Exiting {message}")));
#pragma warning restore CA2000
}
