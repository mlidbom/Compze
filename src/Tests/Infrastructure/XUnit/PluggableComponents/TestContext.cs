using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

readonly record struct TestContextData(Tessaging.Hosting.Testing.PluggableComponents? PluggableComponents, ITestMethod TestMethod)
{
}

static class TestContext
{
   internal static readonly AsyncLocal<TestContextData?> Current = new();

   internal static async Task<RunSummary> RunTestInContextAsync(
      TestContextData contextData,
      Func<Task<RunSummary>> executeTest)
   {
      Current.Value = contextData;
      try
      {
         return await executeTest();
      }
      finally
      {
         Current.Value = null;
      }
   }
}
