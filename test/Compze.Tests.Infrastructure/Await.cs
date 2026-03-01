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

   public static async Task<T> NotNullAsync<T>(Func<T?> tryGetValue, TimeSpan timeout, TimeSpan pollInterval, string failureTessage) where T : class
   {
      var deadline = DateTime.UtcNow.Add(timeout);
      while(DateTime.UtcNow < deadline)
      {
         if(tryGetValue() is { } result)
            return result;
         await Task.Delay(pollInterval);
      }

      throw new Exception(failureTessage);
   }
}
