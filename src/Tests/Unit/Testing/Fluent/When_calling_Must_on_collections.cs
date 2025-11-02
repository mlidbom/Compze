using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

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
      => Invoking(() => new List<int> { 1 }.Must().BeEmpty())
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_non_empty_array()
      => Invoking(() => new[] { 1, 2, 3 }.Must().BeEmpty())
        .Must()
        .Throw<AssertionFailedException>();
}

public class When_calling_Must_NotBeEmpty : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_non_empty_collection()
      => new List<int> { 1 }.Must().NotBeEmpty();

   [XF] public void it_does_not_throw_for_non_empty_array()
      => new[] { 1, 2, 3 }.Must().NotBeEmpty();

   [XF] public void it_does_not_throw_for_non_empty_enumerable()
      => Enumerable.Range(1, 5).Must().NotBeEmpty();

   [XF] public void it_throws_for_empty_collection()
      => Invoking(() => new List<int>().Must().NotBeEmpty())
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_empty_array()
      => Invoking(() => new int[0].Must().NotBeEmpty())
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_empty_enumerable()
      => Invoking(() => Enumerable.Empty<string>().Must().NotBeEmpty())
        .Must()
        .Throw<AssertionFailedException>();
}

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
