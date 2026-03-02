using System.Threading.Tasks;
using Compze.Underscore;

namespace Compze.SystemCE.ThreadingCE.TasksCE;

static class TaskUnit
{
   public static async Task<unit> AsUnit(this Task task)
   {
      await task.caf();
      return unit.Value;
   }
}
