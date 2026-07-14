using Compze.Must.Assertions;

#pragma warning disable CA1861

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_HaveCount : UniversalTestBase
{
   [XF] public void it_does_not_throw_when_count_matches()
      => new List<int> { 1, 2, 3 }.Must().HaveCount(3);

   [XF] public void it_does_not_throw_when_count_is_zero()
      =>
         Array.Empty<int>().Must().HaveCount(0);

   [XF] public void it_throws_when_count_is_different()
      => Invoking(() => new[] { 1, 2, 3 }.Must().HaveCount(5))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_when_count_is_different_and_the_message_is()
      => Invoking(() => new[] { 1, 2, 3 }.Must().HaveCount(5))
        .Must()
        .Throw<AssertionFailedException>()
        .Which.Message.Must().Be("""
                                 
                                 --------------------------------------------------
                                 Failing assertion:
                                 --------------------------------------------------
                                 new[] { 1, 2, 3 }.Must().HaveCount(5)
                                 --------------------------------------------------
                                 Expected count to be 5 but it was 3
                                 --------------------------------------------------
                                 new[] { 1, 2, 3 } was a System.Int32[] with:
                                 --------------------------------------------------
                                 JSON:
                                 --------------------------------------------------
                                 {
                                   "$type": "System.Int32[], System.Private.CoreLib",
                                   "$values": [
                                     1,
                                     2,
                                     3
                                   ]
                                 }
                                 --------------------------------------------------
                                 5 was a System.Int32
                                 --------------------------------------------------
                                 """);


   [XF] public void it_throws_when_expected_empty_but_not()
      => Invoking(() => new[] { 1 }.Must().HaveCount(0))
        .Must()
        .Throw<AssertionFailedException>();
}
