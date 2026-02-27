using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.SystemCE;

public static class UncatchableExceptionsGatherer
{
   static List<Exception> _exceptions = [];
   static readonly IMonitorCE MonitorCE = IMonitorCE.WithTimeout(1.Seconds());

   ///<summary>If writing tests to ensure uncatchable exceptions are registered, you need to prevent others from running similar tests at the same time. Use this monitor for that</summary>
   public static readonly IMonitorCE TestingMonitor = IMonitorCE.WithTimeout(1.Seconds());

   public static unit Register(Exception exception) => MonitorCE.Locked(() => _exceptions.Add(exception));

   public static IReadOnlyList<Exception> Exceptions => _exceptions.ToList();

   public static unit ConsumeAndThrowAnyExceptionsGathered() => MonitorCE.Locked(() =>
   {
      var exceptions = _exceptions;
      _exceptions = [];
      if(exceptions.Any())
         throw new AggregateException(exceptions);
   });

   public static unit ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions()
   {
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      return ConsumeAndThrowAnyExceptionsGathered();
   }
}
