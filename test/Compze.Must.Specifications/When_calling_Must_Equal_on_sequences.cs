

#pragma warning disable CA1861
// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_SequenceEqual : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_equal_sequences()
      => new[] { 1, 2, 3 }.Must().SequenceEqual([1, 2, 3]);

   [XF] public void it_throws_for_different_sequences()
      => Invoking(() => new[] { 1, 2, 3 }.Must().SequenceEqual([1, 2, 4]))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_different_lengths()
      => Invoking(() => new[] { 1, 2 }.Must().SequenceEqual([1, 2, 3]))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_works_with_linq_operations()
      => Enumerable.Range(1, 10).Must().SequenceEqual(Enumerable.Range(1, 10));

   public class when_sequences_differ : When_calling_Must_SequenceEqual
   {
      readonly int[] _actual = [1, 2, 3];
      readonly int[] _expected = [1, 2, 4];

      string ExceptionMessage() => Invoking(() => _actual.Must().SequenceEqual(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

      [XF] public void the_full_exception_message_is() =>
         ExceptionMessage().Must().Be(""""

                                      --------------------------------------------------
                                      Failing assertion:
                                      --------------------------------------------------
                                      _actual.Must().SequenceEqual(_expected)
                                      --------------------------------------------------
                                      Diff:
                                      --------------------------------------------------
                                      --- expected
                                      +++ actual
                                      @@ -3,6 +3,6 @@
                                         "$values": [
                                           1,
                                           2,
                                      -    4
                                      +    3
                                         ]
                                       }

                                      --------------------------------------------------
                                      _actual was a System.Int32[] with:
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
                                      _expected was a System.Int32[] with:
                                      --------------------------------------------------
                                      JSON:
                                      --------------------------------------------------
                                      {
                                        "$type": "System.Int32[], System.Private.CoreLib",
                                        "$values": [
                                          1,
                                          2,
                                          4
                                        ]
                                      }
                                      --------------------------------------------------
                                      """");
   }
}
