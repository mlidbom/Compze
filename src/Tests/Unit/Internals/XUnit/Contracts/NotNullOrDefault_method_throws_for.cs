using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.Unit.Internals.XUnit.Contracts;

public class NotNullOrDefault_method_throws_for : AssertionMethodsTest
{
   readonly int? _nullInt = null;

   [XFact] public void null_int() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullOrDefault(_nullInt), _nullInt);
}
