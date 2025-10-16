using System;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.Unit.Internals.Contracts;

public class NotDefault_method : AssertionMethodsTest
{
   readonly Guid _emptyGuid = default;
   [XF] public void Throws_for_default_struct() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotDefault(_emptyGuid), _emptyGuid);
}
