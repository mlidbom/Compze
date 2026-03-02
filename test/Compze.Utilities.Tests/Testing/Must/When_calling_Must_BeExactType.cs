using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_BeExactType : UniversalTestBase
{
   public class with_an_object_of_the_expected_type : When_calling_Must_BeExactType
   {
      [XF] public void it_does_not_throw()
      {
         object value = "string";
         value.Must().BeExactType<string>();
      }
   }

   public class with_an_object_of_a_different_type : When_calling_Must_BeExactType
   {
      [XF] public void it_throws()
      {
         object value = 42;
         Invoking(() => value.Must().BeExactType<string>())
            .Must()
            .Throw<AssertionFailedException>();
      }
   }

   public class with_a_derived_type : When_calling_Must_BeExactType
   {
      class Base;
      class Derived : Base;

      [XF] public void it_throws_when_expecting_base_type()
      {
         object value = new Derived();
         Invoking(() => value.Must().BeExactType<Base>())
            .Must()
            .Throw<AssertionFailedException>();
      }
   }
}
