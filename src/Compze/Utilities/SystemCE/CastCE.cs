namespace Compze.Utilities.SystemCE;

static class CastCE
{
   internal static TTarget CastTo<TTarget>(this object me) => (TTarget)me.NotNull();
}
