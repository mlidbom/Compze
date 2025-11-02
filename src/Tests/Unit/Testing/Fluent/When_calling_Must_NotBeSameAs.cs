using Compze.Core.Public.Infrastructure;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___ReferenceEqual = Compze.Utilities.Testing.Fluent.Must___ReferenceEqual;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_NotBeSameAs : UniversalTestBase
{
   public class with_two_different_objects_with_the_same_value : When_calling_Must_NotBeSameAs
   {
      [XF] public void it_does_not_throw() => Must___ReferenceEqual.NotBeSameAs(__Must.Must(new ValueWrapper<int>(42)), new ValueWrapper<int>(42));
   }

   public class with_two_references_to_the_same_object : When_calling_Must_NotBeSameAs
   {
      readonly ValueWrapper<int> _actual = new(12);

      [XF] public void it_throws() => Invoking(() => Must___ReferenceEqual.NotBeSameAs(__Must.Must(_actual), _actual))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
