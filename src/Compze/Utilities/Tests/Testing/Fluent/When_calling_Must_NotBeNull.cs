using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Fluent;

public class When_calling_Must_NotBeNull : UniversalTestBase
{
   public class with_a_non_null_reference : When_calling_Must_NotBeNull
   {
      [XF] public void it_does_not_throw() => "not null".Must().NotBeNull();
   }

   public class with_a_null_reference_type_value : When_calling_Must_NotBeNull
   {
      readonly string? _actual = null;

      [XF] public void it_throws() => Invoking(() => _actual.Must().NotBeNull())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_null_nullable_value_type : When_calling_Must_NotBeNull
   {
      readonly int? _actual = null;

      [XF] public void it_throws() => Invoking(() => _actual.Must().NotBeNull())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_non_null_nullable_value_type : When_calling_Must_NotBeNull
   {
      readonly int? _value = 42;
      [XF] public void it_does_not_throw() => _value.Must().NotBeNull();
   }
}
