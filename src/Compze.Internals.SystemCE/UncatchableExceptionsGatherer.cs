using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Internals.SystemCE;

public static class UncatchableExceptionsGatherer
{
   static List<Exception> _exceptions = [];
   static readonly ILock LockCE = ILock.New(LockTimeout.Seconds(1));

   ///<summary>If writing tests to ensure uncatchable exceptions are registered, you need to prevent others from running similar tests at the same time. Use this monitor for that</summary>
   public static readonly ILock TestingLock = ILock.New(LockTimeout.Seconds(1));

   public static unit Register(Exception exception) => LockCE.Locked(() => _exceptions.Add(exception));

   static unit ConsumeAndThrowAnyExceptionsGathered() => LockCE.Locked(() =>
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
