using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

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
