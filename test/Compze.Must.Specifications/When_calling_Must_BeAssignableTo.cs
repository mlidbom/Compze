

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_BeAssignableTo : UniversalTestBase
{
   public class with_an_object_of_the_exact_type : When_calling_Must_BeAssignableTo
   {
      [XF] public void it_does_not_throw()
      {
         object value = "string";
         value.Must().BeAssignableTo<string>();
      }
   }

   public class with_an_object_of_a_derived_type : When_calling_Must_BeAssignableTo
   {
      class Base;
      class Derived : Base;

      [XF] public void it_does_not_throw()
      {
         object value = new Derived();
         value.Must().BeAssignableTo<Base>();
      }
   }

   public class with_an_object_that_implements_an_interface : When_calling_Must_BeAssignableTo
   {
      interface ITest;
      class TestClass : ITest;

      [XF] public void it_does_not_throw()
      {
         object value = new TestClass();
         value.Must().BeAssignableTo<ITest>();
      }
   }

   public class with_an_unrelated_type : When_calling_Must_BeAssignableTo
   {
      [XF] public void it_throws()
      {
         object value = 42;
         Invoking(() => value.Must().BeAssignableTo<string>())
            .Must()
            .Throw<AssertionFailedException>();
      }
   }
}
