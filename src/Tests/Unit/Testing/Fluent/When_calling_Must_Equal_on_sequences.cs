using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_SequenceEqual : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_equal_sequences()
      => new[] { 1, 2, 3 }.Must().SequenceEqual(new[] { 1, 2, 3 });

   [XF] public void it_throws_for_different_sequences()
      => Invoking(() => new[] { 1, 2, 3 }.Must().SequenceEqual(new[] { 1, 2, 4 }))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_different_lengths()
      => Invoking(() => new[] { 1, 2 }.Must().SequenceEqual(new[] { 1, 2, 3 }))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_works_with_linq_operations()
      => Enumerable.Range(1, 10).Must().SequenceEqual(Enumerable.Range(1, 10));
}
