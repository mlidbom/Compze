using System;
using System.Threading.Tasks;

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

static class TaskUnit
{
   internal static async Task<unit> AsUnit(this Task task)
   {
      await task.caf();
      return unit.Value;
   }
}
