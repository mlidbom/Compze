using System.Collections.Generic;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_HaveCount : UniversalTestBase
{
   [XF] public void it_does_not_throw_when_count_matches()
      => new List<int> { 1, 2, 3 }.Must().HaveCount(3);

   [XF] public void it_does_not_throw_when_count_is_zero()
      => new int[0].Must().HaveCount(0);

   [XF] public void it_throws_when_count_is_different()
      => Invoking(() => new[] { 1, 2, 3 }.Must().HaveCount(5))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_when_expected_empty_but_not()
      => Invoking(() => new[] { 1 }.Must().HaveCount(0))
        .Must()
        .Throw<AssertionFailedException>();
}
