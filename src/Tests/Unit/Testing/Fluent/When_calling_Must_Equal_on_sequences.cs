using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___Enumerable = Compze.Utilities.Testing.Fluent.Must___Enumerable;

#pragma warning disable CA1861
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_SequenceEqual : UniversalTestBase
{
   [XF] public void it_does_not_throw_for_equal_sequences()
      => Must___Enumerable.Equal(__Must.Must(new[] { 1, 2, 3 }), new[] { 1, 2, 3 });

   [XF] public void it_throws_for_different_sequences()
      => Invoking(() => Must___Enumerable.Equal(__Must.Must(new[] { 1, 2, 3 }), new[] { 1, 2, 4 }))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_for_different_lengths()
      => Invoking(() => Must___Enumerable.Equal(__Must.Must(new[] { 1, 2 }), new[] { 1, 2, 3 }))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_works_with_linq_operations()
      => Must___Enumerable.Equal(__Must.Must(Enumerable.Range(1, 10)), Enumerable.Range(1, 10));

   public class when_sequences_differ : When_calling_Must_SequenceEqual
   {
      readonly int[] _actual = [1, 2, 3];
      readonly int[] _expected = [1, 2, 4];

      string ExceptionMessage() => Invoking(() => Must___Enumerable.Equal(__Must.Must(_actual), _expected)).Must().Throw<AssertionFailedException>().Which.Message;

      [XF] public void the_exception_message_includes_the_full_diff() =>
         ExceptionMessage().Must().Be(""""

                                      --------------------------------------------------
                                      expected the sequence:
                                      --------------------------------------------------
                                         _actual
                                      --------------------------------------------------
                                      to be sequence equal to:
                                      --------------------------------------------------
                                         _expected
                                      --------------------------------------------------
                                      But it was not.
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
                                      Actual was:
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
                                      Expected was:
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
