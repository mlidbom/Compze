// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_StartWith : UniversalTestBase
{
   public class with_the_strings_actual_beginning : When_calling_Must_StartWith
   {
      [XF] public void it_does_not_throw() => "hello world".Must().StartWith("hello");
   }

   public class with_a_string_the_actual_value_does_not_start_with : When_calling_Must_StartWith
   {
      [XF] public void it_throws() => Invoking(() => "hello world".Must().StartWith("world"))
                                     .Must()
                                     .Throw<AssertionFailedException>();

      [XF] public void the_message_contains_the_expected_beginning() =>
         Invoking(() => "hello world".Must().StartWith("world"))
            .Must()
            .Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("world");
   }
}
