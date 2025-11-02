using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___TypeOfActual = Compze.Utilities.Testing.Fluent.Must___TypeOfActual;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeExactType : UniversalTestBase
{
   public class with_an_object_of_the_expected_type : When_calling_Must_BeExactType
   {
      [XF] public void it_does_not_throw()
      {
         object value = "string";
         Must___TypeOfActual.BeOfType<string>(__Must.Must(value));
      }
   }

   public class with_an_object_of_a_different_type : When_calling_Must_BeExactType
   {
      [XF] public void it_throws()
      {
         object value = 42;
         Invoking(() => Must___TypeOfActual.BeOfType<string>(__Must.Must(value)))
            .Must()
            .Throw<AssertionFailedException>();
      }
   }

   public class with_a_derived_type : When_calling_Must_BeExactType
   {
      class Base { }
      class Derived : Base { }

      [XF] public void it_throws_when_expecting_base_type()
      {
         object value = new Derived();
         Invoking(() => Must___TypeOfActual.BeOfType<Base>(__Must.Must(value)))
            .Must()
            .Throw<AssertionFailedException>();
      }
   }
}
