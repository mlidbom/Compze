using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_BeOneOf = Compze.Utilities.Testing.Fluent.Must_BeOneOf;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeOneOf : UniversalTestBase
{
   public class with_a_value_in_the_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_does_not_throw() => Must_BeOneOf.BeOneOf(__Must.Must(2), [1, 2, 3]);
   }

   public class with_a_value_not_in_the_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_throws() => Invoking(() => Must_BeOneOf.BeOneOf(__Must.Must(5), [1, 2, 3]))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_strings : When_calling_Must_BeOneOf
   {
      [XF] public void it_works_with_reference_types() => Must_BeOneOf.BeOneOf(__Must.Must("b"), ["a", "b", "c"]);
   }

   public class with_empty_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_throws() => Invoking(() => Must_BeOneOf.BeOneOf(__Must.Must(1), []))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
