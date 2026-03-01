using System.Threading.Tasks;
using Compze.Functional;

namespace Compze.Threading.TasksCE;

public static class TaskUnit
{
   public static async Task<unit> AsUnit(this Task task)
   {
      await task.caf();
      return unit.Value;
   }
}
