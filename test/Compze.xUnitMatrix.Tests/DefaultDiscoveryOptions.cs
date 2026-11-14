using Xunit.Sdk;

namespace Compze.xUnitMatrix.Tests;

///<summary>The discovery options of a test runner that sets none, so that xUnit's default applies to every option.</summary>
sealed class DefaultDiscoveryOptions : ITestFrameworkDiscoveryOptions
{
   public TValue? GetValue<TValue>(string name) => default;

   public void SetValue<TValue>(string name, TValue value) => throw new NotSupportedException($"{nameof(DefaultDiscoveryOptions)} sets no option.");

   public string ToJson() => throw new NotSupportedException($"{nameof(DefaultDiscoveryOptions)} is only read by discovery.");
}
