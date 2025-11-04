using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

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
