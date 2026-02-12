using System;
using System.Collections.Generic;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LongRunningAttribute : Attribute, ITraitAttribute
{
   public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new("Category", "LongRunning")];
}
