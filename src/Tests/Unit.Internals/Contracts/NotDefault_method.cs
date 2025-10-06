using System;
using Compze.Testing.TestFrameworkExtensions.XUnit;

namespace Compze.Tests.Unit.Internals.Contracts;

public class NotDefault_method : AssertionMethodsTest
{
   readonly Guid _emptyGuid = default;
   [XFact] public void Throws_for_default_struct() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotDefault(_emptyGuid), _emptyGuid);
}
