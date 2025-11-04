using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1861

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_BeEmpty : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_empty_collection()
      => new List<int>().Must().BeEmpty();

   [XF] public void it_does_not_throw_for_empty_array()
      => Array.Empty<int>().Must().BeEmpty();

   [XF] public void it_does_not_throw_for_empty_enumerable()
      => Enumerable.Empty<string>().Must().BeEmpty();

   [XF] public void it_throws_for_non_empty_collection()
      => MustActions.Invoking(() => new List<int> { 1 }.Must().BeEmpty())
                    .Must()
                    .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_non_empty_array()
      => MustActions.Invoking(() => new[] { 1, 2, 3 }.Must().BeEmpty())
                    .Must()
                    .Throw<AssertionFailedException>();
}
