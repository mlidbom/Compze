using System;
using System.Threading.Tasks;
using Compze.Utilities.Functional;

namespace Compze.Threading.TasksCE;

static class TaskUnit
{
   internal static async Task<unit> AsUnit(this Task task)
   {
      await task.caf();
      return unit.Value;
   }
}
