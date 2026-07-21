// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_EndWith : UniversalTestBase
{
   public class with_the_strings_actual_ending : When_calling_Must_EndWith
   {
      [XF] public void it_does_not_throw() => "hello world".Must().EndWith("world");
   }

   public class with_a_string_the_actual_value_does_not_end_with : When_calling_Must_EndWith
   {
      [XF] public void it_throws() => Invoking(() => "hello world".Must().EndWith("hello"))
                                     .Must()
                                     .Throw<AssertionFailedException>();

      [XF] public void the_message_contains_the_expected_ending() =>
         Invoking(() => "hello world".Must().EndWith("hello"))
            .Must()
            .Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("hello");
   }
}
