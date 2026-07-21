using Compze.SystemCE;

namespace Compze.Internals.SystemCE.ThreadingCE.TasksCE._private;

static class TaskUnit
{
   public static async Task<Unit> AsUnit(this Task task)
   {
      await task.caf();
      return unit;
   }
}
