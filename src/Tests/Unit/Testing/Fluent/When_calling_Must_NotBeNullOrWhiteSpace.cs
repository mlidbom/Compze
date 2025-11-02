using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

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
