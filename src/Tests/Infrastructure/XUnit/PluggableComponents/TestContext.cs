using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

public readonly record struct TestContextData(Tessaging.Hosting.Testing.PluggableComponents? PluggableComponents, ITestMethod TestMethod)
{
}

public static class TestContext
{
   public static TestContextData? Current => CurrentInternal.Value;
   static readonly AsyncLocal<TestContextData?> CurrentInternal = new();

   internal static async Task<RunSummary> RunTestInContextAsync(
      TestContextData contextData,
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
