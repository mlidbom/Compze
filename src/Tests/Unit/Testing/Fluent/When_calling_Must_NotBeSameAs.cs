using Compze.Core.Public.Infrastructure;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using Compze.Utilities.Testing.Fluent;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_NotBeSameAs : UniversalTestBase
{
   public class with_two_different_objects_with_the_same_value : When_calling_Must_NotBeSameAs
   {
      [XF] public void it_does_not_throw() => new ValueWrapper<int>(42).Must().NotBeSameAs(new ValueWrapper<int>(42));
   }

   public class with_two_references_to_the_same_object : When_calling_Must_NotBeSameAs
   {
      readonly ValueWrapper<int> _actual = new(12);

      [XF] public void it_throws() => Invoking(() => _actual.Must().NotBeSameAs(_actual))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
