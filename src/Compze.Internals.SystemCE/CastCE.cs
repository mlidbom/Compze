using Compze.Contracts;

namespace Compze.Internals.SystemCE;

public static class CastCE
{
   public static TTarget CastTo<TTarget>(this object me) => (TTarget)me._assert().NotNull();
}
