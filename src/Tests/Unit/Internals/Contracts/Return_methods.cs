using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

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
      [XF] public void throws_for_null_int() => Invoking(() => Asserter.ReturnNotNull(NullInt))
                                                  .Must().Throw<AssertionTestException>()
                                                  .Which.Message.Must().Contain(nameof(NullInt));

      [XF] public void throws_for_null_object() => Invoking(() => Asserter.ReturnNotNull(NullObject))
                                                     .Must().Throw<AssertionTestException>()
                                                     .Which.Message.Must().Contain(nameof(NullObject));

      [XF] public void Returns_the_value_if_valid() => Asserter.ReturnNotNull(EmptyObject).Must().Be(EmptyObject);
   }

   public class ReturnNotNullOrDefault_method
   {
      [XF] public void throws_for_null_int() => Invoking(() => Asserter.ReturnNotNullOrDefault(NullInt))
                                                  .Must().Throw<AssertionTestException>()
                                                  .Which.Message.Must().Contain(nameof(NullInt));

      [XF] public void throws_for_default_int() => Invoking(() => Asserter.ReturnNotNullOrDefault(DefaultInt))
                                                     .Must().Throw<AssertionTestException>()
                                                     .Which.Message.Must().Contain(nameof(DefaultInt));

      [XF] public void Returns_the_value_if_valid() => Asserter.ReturnNotNull(AnInt).Must().Be(AnInt);
   }
}
