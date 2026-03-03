using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PerformanceAttribute : Attribute, ITraitAttribute
{
   public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new("Category", "Performance")];
}
