using System;
using System.Threading.Tasks;

namespace Compze.Tests.Infrastructure;

public static class Await
{
   public static async Task Async(Func<bool> condition, TimeSpan timeout, TimeSpan pollInterval, string failureTessage)
   {
      var deadline = DateTime.UtcNow.Add(timeout);
      while(DateTime.UtcNow < deadline)
      {
         if(condition())
            return;
         await Task.Delay(pollInterval);
      }

      throw new Exception(failureTessage);
   }
}
