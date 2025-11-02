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
      readonly string _actual = "not null";

      [XF] public void NotBeNull_does_not_throw() => _actual.Must().NotBeNull();
   }

   public class given_a_null_reference : When_using_NotBeNull
   {
      readonly string? _actual = null;

      public class NotBeNull_throws_AssertionFailedException : given_a_null_reference
      {
         [XF] public void when_value_is_null()
            => Invoking(() => _actual.Must().NotBeNull()).Must().Throw<AssertionFailedException>();

         string ExceptionMessage() => Invoking(() => _actual.Must().NotBeNull())
                                     .Must()
                                     .Throw<AssertionFailedException>()
                                     .Which
                                     .Message;

         [XF] public void is_the_full_formatted_message()
            => ExceptionMessage().Must().Be("""
                                            --------------------------------------------------
                                            expected the object "it" returned by the expression: 
                                            --------------------------------------------------
                                               _actual
                                            --------------------------------------------------
                                            to not be null, but it was null
                                            --------------------------------------------------
                                            """);
      }
   }

   public class given_a_nullable_value_type : When_using_NotBeNull
   {
      [XF] public void NotBeNull_works_with_nullable_structs()
      {
         int? value = 42;
         value.Must().NotBeNull();
      }
   }
}
