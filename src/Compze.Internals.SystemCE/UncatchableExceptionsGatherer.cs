using Compze.SystemCE;
using Compze.Threading;
using Compze.Internals.SystemCE.Private;

namespace Compze.Internals.SystemCE;

public static class UncatchableExceptionsGatherer
{
   static List<Exception> _exceptions = [];
   static readonly IMonitor Monitor = IMonitor.New(LockTimeout.Seconds(1));

   ///<summary>If writing tests to ensure uncatchable exceptions are registered, you need to prevent others from running similar tests at the same time. Use this monitor for that</summary>
   public static readonly IMonitor TestingMonitor = IMonitor.New(LockTimeout.Seconds(1));

   public static Unit Register(Exception exception) => Monitor.Locked(() => _exceptions.Add(exception));

   static Unit ConsumeAndThrowAnyExceptionsGathered() => Monitor.Locked(() =>
   {
      var exceptions = _exceptions;
      _exceptions = [];
      if(exceptions.Any())
         throw new AggregateException(exceptions);
   });

   public static Unit ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions()
   {
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      return ConsumeAndThrowAnyExceptionsGathered();
   }
}
