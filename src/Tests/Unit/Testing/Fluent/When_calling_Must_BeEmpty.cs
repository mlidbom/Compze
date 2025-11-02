using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeEmpty : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_empty_collection()
      => new List<int>().Must().BeEmpty();

   [XF] public void it_does_not_throw_for_empty_array()
      => new int[0].Must().BeEmpty();

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
