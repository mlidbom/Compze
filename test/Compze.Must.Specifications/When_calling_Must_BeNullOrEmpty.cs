using Compze.Must.Assertions;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_BeNullOrEmpty : UniversalTestBase
{
   public class with_null : When_calling_Must_BeNullOrEmpty
   {
      readonly string? _value = null;
      [XF] public void it_does_not_throw() => _value.Must().BeNullOrEmpty();
   }

   public class with_empty_string : When_calling_Must_BeNullOrEmpty
   {
      [XF] public void it_does_not_throw() => string.Empty.Must().BeNullOrEmpty();
   }

   public class with_non_empty_string : When_calling_Must_BeNullOrEmpty
   {
      [XF] public void it_throws() => Invoking(() => "text".Must().BeNullOrEmpty())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
