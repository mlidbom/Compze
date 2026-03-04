using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_BeNull : UniversalTestBase
{
   public class with_a_null_reference : When_calling_Must_BeNull
   {
      readonly string? _actual = null;
      [XF] public void it_does_not_throw() => _actual.Must().BeNull();
   }

   public class with_a_non_null_reference : When_calling_Must_BeNull
   {
      readonly string? _actual = "not null";

      [XF] public void it_throws() =>
         Invoking(() => _actual.Must().BeNull())
           .Must()
           .Throw<AssertionFailedException>();
   }

   public class with_a_nullable_value_type_that_is_null : When_calling_Must_BeNull
   {
      [XF] public void it_does_not_throw()
      {
         int? value = null;
         value.Must().BeNull();
      }
   }
}
