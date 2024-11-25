using System;
using System.Runtime.CompilerServices;
using Composable.Functional;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;

namespace Composable.Logging;

static class LevelLoggerExtensions
{
   public static IDisposable LogMethodEntryExit(this ILevelLogger @this, [CallerMemberName] string message = "") =>
      @this.Log($"Entering {message}")
           .then(DisposableCE.Create(() => @this.Log($"Exiting {message}")));

   public static IDisposable LogEntryExit(this ILevelLogger @this, string message = "") =>
      @this.Log($"Entering {message}")
           .then(DisposableCE.Create(() => @this.Log($"Exiting {message}")));
}
