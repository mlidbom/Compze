using System;
using System.Collections.Generic;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

#pragma warning disable CA1861

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

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
                                 new[] { 1, 2, 3 }.Must().HaveCount()
                                 --------------------------------------------------
                                 "it" defined by:
                                 --------------------------------------------------
                                    new[] { 1, 2, 3 }
                                 --------------------------------------------------
                                 failed to Satisfy:
                                 --------------------------------------------------
                                    Count == 5
                                 --------------------------------------------------
                                 but Count was: 3, not 5
                                 --------------------------------------------------
                                 "it" was:
                                 --------------------------------------------------
                                 ToString():
                                 --------------------------------------------------
                                 System.Int32[]
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
                                 """);


   [XF] public void it_throws_when_expected_empty_but_not()
      => Invoking(() => new[] { 1 }.Must().HaveCount(0))
        .Must()
        .Throw<AssertionFailedException>();
}
