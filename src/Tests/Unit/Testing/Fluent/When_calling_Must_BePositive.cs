using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___IComparableAssertions = Compze.Utilities.Testing.Fluent.Must___IComparableAssertions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BePositive : UniversalTestBase
{
   public class with_a_positive_number : When_calling_Must_BePositive
   {
      [XF] public void it_does_not_throw() => Must___IComparableAssertions.BePositive(__Must.Must(42));
   }

   public class with_zero : When_calling_Must_BePositive
   {
      [XF] public void it_throws() => Invoking(() => Must___IComparableAssertions.BePositive(__Must.Must(0)))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_negative_number : When_calling_Must_BePositive
   {
      [XF] public void it_throws() => Invoking(() => Must___IComparableAssertions.BePositive(__Must.Must((-5))))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
