using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___TypeOfActual = Compze.Utilities.Testing.Fluent.Must___TypeOfActual;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeAssignableTo : UniversalTestBase
{
   public class with_an_object_of_the_exact_type : When_calling_Must_BeAssignableTo
   {
      [XF] public void it_does_not_throw()
      {
         object value = "string";
         Must___TypeOfActual.BeAssignableTo<string>(__Must.Must(value));
      }
   }

   public class with_an_object_of_a_derived_type : When_calling_Must_BeAssignableTo
   {
      class Base { }
      class Derived : Base { }

      [XF] public void it_does_not_throw()
      {
         object value = new Derived();
         Must___TypeOfActual.BeAssignableTo<Base>(__Must.Must(value));
      }
   }

   public class with_an_object_that_implements_an_interface : When_calling_Must_BeAssignableTo
   {
      interface ITest { }
      class TestClass : ITest { }

      [XF] public void it_does_not_throw()
      {
         object value = new TestClass();
         Must___TypeOfActual.BeAssignableTo<ITest>(__Must.Must(value));
      }
   }

   public class with_an_unrelated_type : When_calling_Must_BeAssignableTo
   {
      [XF] public void it_throws()
      {
         object value = 42;
         Invoking(() => Must___TypeOfActual.BeAssignableTo<string>(__Must.Must(value)))
            .Must()
            .Throw<AssertionFailedException>();
      }
   }
}
