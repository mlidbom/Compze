using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_NotBeNull = Compze.Utilities.Testing.Fluent.Must_NotBeNull;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

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
