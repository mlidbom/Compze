using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Testing;

public class TaskCETesting
{
   async Task<T> CompletesWithValueIn<T>(T value, int milliseconds)
   {
      await Task.Delay(milliseconds).NoMarshalling();
      return value;
   }
}
