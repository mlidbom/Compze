// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_NotBeNullOrWhiteSpace : UniversalTestBase
{
   public class with_non_whitespace_string : When_calling_Must_NotBeNullOrWhiteSpace
   {
      [XF] public void it_does_not_throw() => "text".Must().NotBeNullOrWhiteSpace();
   }

   public class with_null : When_calling_Must_NotBeNullOrWhiteSpace
   {
      readonly string? _value = null;
      [XF] public void it_throws() => Invoking(() => _value.Must().NotBeNullOrWhiteSpace())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_empty_string : When_calling_Must_NotBeNullOrWhiteSpace
   {
      [XF] public void it_throws() => Invoking(() => string.Empty.Must().NotBeNullOrWhiteSpace())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_whitespace_only : When_calling_Must_NotBeNullOrWhiteSpace
   {
      [XF] public void it_throws() => Invoking(() => "   ".Must().NotBeNullOrWhiteSpace())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
