namespace Compze.Internals.SystemCE;

static class ActionCE
{
   public static void InvokeAll(this IEnumerable<Action> @this) => @this.ForEach(me => me.Invoke());
}