using System.Threading.Tasks;
using Compze.Functional;

namespace Compze.SystemCE.ThreadingCE.TasksCE;

static class TaskUnit
{
   internal static async Task<Unit> AsUnit(this Task task)
   {
      await task.CaF();
      return Unit.Instance;
   }
}
