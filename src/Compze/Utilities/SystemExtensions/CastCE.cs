using Compze.Utilities.SystemCE;

namespace Compze.Utilities;

static class CastCE
{
   internal static TTarget CastTo<TTarget>(this object me) => (TTarget)me.NotNull();
}
