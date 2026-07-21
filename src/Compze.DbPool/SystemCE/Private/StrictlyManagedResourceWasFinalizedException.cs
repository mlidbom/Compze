using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.DbPool.SystemCE.Private;

///<summary><see cref="IStrictlyManagedResource"/></summary>
class StrictlyManagedResourceWasFinalizedException(Type instanceType, string? reservationCallStack) : Exception(FormatTessage(instanceType, reservationCallStack))
{
   static string FormatTessage(Type instanceType, string? reservationCallStack)
      => !reservationCallStack.IsNullEmptyOrWhiteSpace()
            ? $"""
               User code failed to Dispose this instance of {instanceType.GetFullNameCompilable()}
               Construction call stack: {reservationCallStack}
               """
            : $"""
               No allocation stack trace collected. 
               Set {instanceType.FullName}.{nameof(StrictlyManagedResources.CollectStackTracesByDefault)} == true to collect allocation stack traces for this type.
               Set: {nameof(StrictlyManagedResources)}.{nameof(StrictlyManagedResources.CollectStackTracesByDefault)} == true to collect allocation stack traces for all strictly managed resources.
               Please note that this will decrease performance and should only be set while debugging resource leaks.
               """;
}
