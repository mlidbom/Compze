using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_BeNull : UniversalTestBase
{
   public class given_a_null_reference : When_using_BeNull
   {
      readonly string? _actual = null;

      [XF] public void BeNull_does_not_throw() => _actual.Must().BeNull();
   }

   public class given_a_non_null_reference : When_using_BeNull
   {
      readonly string _actual = "not null";

      public class BeNull_throws_AssertionFailedException : given_a_non_null_reference
      {
         [XF] public void when_value_is_not_null()
            => Invoking(() => _actual.Must().BeNull()).Must().Throw<AssertionFailedException>();

         string ExceptionMessage() => Invoking(() => _actual.Must().BeNull())
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
                                            to be null, but it was not null
                                            --------------------------------------------------
                                            """);
      }
   }

   public class given_a_nullable_value_type : When_using_BeNull
   {
      [XF] public void BeNull_works_with_nullable_structs()
      {
         int? value = null;
         value.Must().BeNull();
      }
   }
}
