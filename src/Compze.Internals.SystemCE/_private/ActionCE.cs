namespace Compze.Internals.SystemCE._private;

static class ActionCE
{
   public static void InvokeAll(this IEnumerable<Action> @this) => @this.ForEach(me => me.Invoke());
}