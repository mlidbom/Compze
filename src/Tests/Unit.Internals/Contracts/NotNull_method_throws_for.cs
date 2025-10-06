using Compze.Testing.TestFrameworkExtensions.XUnit;

namespace Compze.Tests.Unit.Internals.Contracts;

public class NotNull_method_throws_for : AssertionMethodsTest
{
   static readonly string? NullString = null;
   static readonly object? NullObject = null;

   [XFact] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNull(NullString), NullString);
   [XFact] public void null_object() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNull(NullObject), NullObject);
}
