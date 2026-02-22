namespace Compze.Utilities.SystemCE;

internal static class CastCE
{
   public static TTarget CastTo<TTarget>(this object me) => (TTarget)me.NotNull();
}
