using System;
using System.Collections.Generic;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___Enumerable = Compze.Utilities.Testing.Fluent.Must___Enumerable;

#pragma warning disable CA1861

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_HaveCount : UniversalTestBase
{
   [XF] public void it_does_not_throw_when_count_matches()
      => Must___Enumerable.HaveCount(__Must.Must(new List<int> { 1, 2, 3 }), 3);

   [XF] public void it_does_not_throw_when_count_is_zero()
      =>
         Must___Enumerable.HaveCount(__Must.Must(Array.Empty<int>()), 0);

   [XF] public void it_throws_when_count_is_different()
      => Invoking(() => Must___Enumerable.HaveCount(__Must.Must(new[] { 1, 2, 3 }), 5))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_when_expected_empty_but_not()
      => Invoking(() => Must___Enumerable.HaveCount(__Must.Must(new[] { 1 }), 0))
        .Must()
        .Throw<AssertionFailedException>();
}
