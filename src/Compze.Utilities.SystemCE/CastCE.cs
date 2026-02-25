using Compze.Contracts;

namespace Compze.Utilities.SystemCE;

public static class CastCE
{
   public static TTarget CastTo<TTarget>(this object me) => (TTarget)me._assertNotNull();
}
