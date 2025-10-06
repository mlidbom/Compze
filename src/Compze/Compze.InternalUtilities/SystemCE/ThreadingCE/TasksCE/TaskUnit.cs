using System.Threading.Tasks;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

static class TaskUnit
{
   internal static async Task<Unit> AsUnit(this Task task)
   {
      await task.caf();
      return Unit.Instance;
   }
}
