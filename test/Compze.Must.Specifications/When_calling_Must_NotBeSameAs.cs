using Compze.Core.Public.Infrastructure;
using AssertionFailedException = Compze.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_NotBeSameAs : UniversalTestBase
{
   public class with_two_different_objects_with_the_same_value : When_calling_Must_NotBeSameAs
   {
      [XF] public void it_does_not_throw() => new ValueWrapper<int>(42).Must().NotReferenceEqual(new ValueWrapper<int>(42));
   }

   public class with_two_references_to_the_same_object : When_calling_Must_NotBeSameAs
   {
      readonly ValueWrapper<int> _actual = new(12);

      [XF] public void it_throws() => Invoking(() => _actual.Must().NotReferenceEqual(_actual))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
