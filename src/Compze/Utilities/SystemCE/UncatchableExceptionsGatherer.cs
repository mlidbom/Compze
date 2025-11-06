using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.SystemCE;

static class UncatchableExceptionsGatherer
{
   static List<Exception> _exceptions = [];
   static readonly LockCE Monitor = LockCE.WithTimeout(1.Seconds());

   ///<summary>If writing tests to ensure uncatchable exceptions are registered, you need to prevent others from running similar tests at the same time. Use this monitor for that</summary>
   internal static readonly LockCE TestingMonitor = LockCE.WithTimeout(1.Seconds());

   internal static unit Register(Exception exception) => Monitor.Update(() => _exceptions.Add(exception));

   internal static IReadOnlyList<Exception> Exceptions => _exceptions.ToList();

   internal static unit ConsumeAndThrowAnyExceptionsGathered() => Monitor.Update(() =>
   {
      var exceptions = _exceptions;
      _exceptions = [];
      if(exceptions.Any())
         throw new AggregateException(exceptions);
   });

   internal static unit ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions()
   {
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      return ConsumeAndThrowAnyExceptionsGathered();
   }
}
