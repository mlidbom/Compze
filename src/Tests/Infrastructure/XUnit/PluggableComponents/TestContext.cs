using System.Threading;
using Xunit.Abstractions;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

public readonly record struct TestContextData(Tessaging.Hosting.Testing.PluggableComponents? PluggableComponents, ITestMethod TestMethod)
{
}

static class TestContext
{
   internal static readonly AsyncLocal<TestContextData?> Current = new();
}
