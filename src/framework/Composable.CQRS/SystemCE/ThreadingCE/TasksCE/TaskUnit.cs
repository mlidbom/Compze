using System.Threading.Tasks;
using Composable.Functional;

namespace Composable.SystemCE.ThreadingCE.TasksCE;

static class TaskUnit
{
   internal static async Task<Unit> AsUnit(this Task task)
   {
      await task.CaF();
      return Unit.Instance;
   }
}
