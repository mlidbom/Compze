using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Compze.Functional;
using Compze.SystemCE;
// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

namespace Compze.Logging;

static class LevelLoggerExtensions
{
   public static IDisposable LogMethodEntryExit(this ILevelLogger @this, [CallerMemberName] string message = "") =>
      @this.Log($"Entering {message}")
           .then(DisposableCE.Create(() => @this.Log($"Exiting {message}")));

   public static IDisposable LogMethodExecutionTime(this ILevelLogger @this, [CallerMemberName] string message = "")
      => Stopwatch.StartNew().select(it => DisposableCE.Create(() => @this.Log($"Executed {message} in {it.Elapsed}")));

   public static IDisposable LogEntryExit(this ILevelLogger @this, string message = "") =>
      @this.Log($"Entering {message}")
           .then(DisposableCE.Create(() => @this.Log($"Exiting {message}")));
}
