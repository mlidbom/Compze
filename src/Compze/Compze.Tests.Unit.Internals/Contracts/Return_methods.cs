using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Contracts;

public class Return_methods : AssertionMethodsTest
{
   static readonly int? NullInt = null;
   static readonly int? AnInt = 1;
   static readonly int? DefaultInt = 0;
   static readonly object? NullObject = null;
   static readonly object? EmptyObject = new();

   public class ReturnNotNull_method
   {
      [XFact] public void throws_for_null_int() => Invoking(() => Asserter.ReturnNotNull(NullInt))
                                                  .Should().Throw<AssertionTestException>()
                                                  .Which.Message.Should().Contain(nameof(NullInt));

      [XFact] public void throws_for_null_object() => Invoking(() => Asserter.ReturnNotNull(NullObject))
                                                     .Should().Throw<AssertionTestException>()
                                                     .Which.Message.Should().Contain(nameof(NullObject));

      [XFact] public void Returns_the_value_if_valid() => Asserter.ReturnNotNull(EmptyObject).Should().Be(EmptyObject);
   }

   public class ReturnNotNullOrDefault_method
   {
      [XFact] public void throws_for_null_int() => Invoking(() => Asserter.ReturnNotNullOrDefault(NullInt))
                                                  .Should().Throw<AssertionTestException>()
                                                  .Which.Message.Should().Contain(nameof(NullInt));

      [XFact] public void throws_for_default_int() => Invoking(() => Asserter.ReturnNotNullOrDefault(DefaultInt))
                                                     .Should().Throw<AssertionTestException>()
                                                     .Which.Message.Should().Contain(nameof(DefaultInt));

      [XFact] public void Returns_the_value_if_valid() => Asserter.ReturnNotNull(AnInt).Should().Be(AnInt);
   }
}
