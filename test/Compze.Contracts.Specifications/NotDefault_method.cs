using System;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Contracts.Specifications;

public class NotDefault_method : AssertionMethodsTest
{
   readonly Guid _emptyGuid = default;
   [XF] public void Throws_for_default_struct() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotDefault(_emptyGuid), _emptyGuid);
}
