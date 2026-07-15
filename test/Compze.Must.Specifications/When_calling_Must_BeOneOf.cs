

// ReSharper disable InconsistentNaming



namespace Compze.Must.Specifications;

public class When_calling_Must_BeOneOf : UniversalTestBase
{
   public class with_a_value_in_the_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_does_not_throw() => 2.Must().BeOneOf([1, 2, 3]);
   }

   public class with_a_value_not_in_the_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_throws() => Invoking(() => 5.Must().BeOneOf([1, 2, 3]))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_strings : When_calling_Must_BeOneOf
   {
      [XF] public void it_works_with_reference_types() => "b".Must().BeOneOf(["a", "b", "c"]);
   }

   public class with_empty_list : When_calling_Must_BeOneOf
   {
      [XF] public void it_throws() => Invoking(() => 1.Must().BeOneOf([]))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
