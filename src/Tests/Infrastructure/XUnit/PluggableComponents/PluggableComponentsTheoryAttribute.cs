using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Use this attribute instead of [Fact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// </summary>
[XunitTestCaseDiscoverer("Compze.Tests.Infrastructure.XUnit.PluggableComponents.PluggableComponentsTheoryDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class PluggableComponentsTheoryAttribute : FactAttribute
{
   /// <summary>
   /// SQL layers to exclude from test execution. Use when a test is not applicable to certain database types.
   /// </summary>
   public Wiring.SqlLayer[] ExcludeSqlLayers { get; init; } = [];
}
#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
