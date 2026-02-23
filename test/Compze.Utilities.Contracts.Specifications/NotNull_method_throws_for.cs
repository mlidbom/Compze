using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Contracts.Specifications;

public class NotNull_method_throws_for : AssertionMethodsTest
{
   static readonly string? NullString = null;
   static readonly object? NullObject = null;

   [XF] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNull(NullString), NullString);
   [XF] public void null_object() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNull(NullObject), NullObject);
}
