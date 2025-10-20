using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit;


public static class TestContext
{
   public static XunitTestCase? CurrentTestCase => CurrentInternal.Value;
   static readonly AsyncLocal<XunitTestCase?> CurrentInternal = new();

   internal static async Task<RunSummary> RunTestInContextAsync(
      XunitTestCase contextData,
      Func<Task<RunSummary>> executeTest)
   {
      CurrentInternal.Value = contextData;
      try
      {
         return await executeTest();
      }
      finally
      {
         CurrentInternal.Value = null;
      }
   }
}
