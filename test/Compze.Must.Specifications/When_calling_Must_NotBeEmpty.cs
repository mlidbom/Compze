using Compze.Must.Assertions;

#pragma warning disable CA1861
#pragma warning disable CA1825

namespace Compze.Must.Specifications;

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
      => Invoking(() => Array.Empty<int>().Must().NotBeEmpty())
                    .Must()
                    .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_empty_enumerable()
      => Invoking(() => Enumerable.Empty<string>().Must().NotBeEmpty())
                    .Must()
                    .Throw<AssertionFailedException>();
}
