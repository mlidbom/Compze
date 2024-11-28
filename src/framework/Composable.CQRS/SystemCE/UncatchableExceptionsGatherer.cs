using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Functional;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE;

static class UncatchableExceptionsGatherer
{
   static List<Exception> _exceptions = [];
   static readonly MonitorCE Monitor = MonitorCE.WithTimeout(1.Seconds());

   ///<summary>If writing tests to ensure uncatchable exceptions are registered, you need to prevent others from running similar tests at the same time. Use this monitor for that</summary>
   internal static readonly MonitorCE TestingMonitor = MonitorCE.WithTimeout(1.Seconds());

   internal static Unit Register(Exception exception) => Monitor.Update(() => _exceptions.Add(exception));

   internal static IReadOnlyList<Exception> Exceptions => _exceptions.ToList();

   internal static IReadOnlyList<Exception> Consume() => Monitor.Update(() =>
   {
      var exceptions = _exceptions;
      _exceptions = [];
      return exceptions.ToArray();
   });

   internal static IReadOnlyList<Exception> ForceGcCollectionWaitForFinalizersAndConsumeErrors()
   {
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      return Consume();
   }
}
