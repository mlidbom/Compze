using Compze.Abstractions.Public.Infrastructure;
using Compze.Must.Assertions;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_BeSameAs : UniversalTestBase
{
   public class with_two_different_objects_with_the_same_value : When_calling_Must_BeSameAs
   {
      [XF] public void it_throws() =>
         Invoking(() => new ValueWrapper<int>(42).Must().ReferenceEqual(new ValueWrapper<int>(42)))
           .Must()
           .Throw<AssertionFailedException>();
   }

   public class with_two_references_to_the_same_object : When_calling_Must_BeSameAs
   {
      readonly ValueWrapper<int> _actual = new(12);

      [XF] public void it_does_not_throw() => _actual
                                                   .Must()
                                                   .ReferenceEqual(_actual);
   }
}
