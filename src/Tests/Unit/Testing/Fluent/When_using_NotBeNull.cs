using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_NotBeNull : UniversalTestBase
{
   public class given_a_non_null_reference : When_using_NotBeNull
   {
      [XF] public void NotBeNull_does_not_throw() => "not null".Must().NotBeNull();
   }

   public class given_a_null_reference_type : When_using_NotBeNull
   {
      readonly string? _actual = null;

      [XF] public void Not_be_null_throwAssertionFailedException() =>
         Invoking(() => _actual.Must().NotBeNull())
           .Must()
           .Throw<AssertionFailedException>();
   }

   public class given_a_null_nullable_value_type : When_using_NotBeNull
   {
      readonly int? _actual = null;

      [XF] public void Not_be_null_throwAssertionFailedException() =>
         Invoking(() => _actual.Must().NotBeNull())
           .Must()
           .Throw<AssertionFailedException>();
   }

   public class given_a_non_null_nullable_value_type : When_using_NotBeNull
   {
      [XF] public void NotBeNull_does_not_throw()
      {
         int? value = 42;
         value.Must().NotBeNull();
      }
   }
}
