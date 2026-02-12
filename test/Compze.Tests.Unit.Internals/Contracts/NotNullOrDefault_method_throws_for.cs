using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.Internals.Contracts;

public class NotNullOrDefault_method_throws_for : AssertionMethodsTest
{
   readonly int? _nullInt = null;

   [XF] public void null_int() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullOrDefault(_nullInt), _nullInt);
}
