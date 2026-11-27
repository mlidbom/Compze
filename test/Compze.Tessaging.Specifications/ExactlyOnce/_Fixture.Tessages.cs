using Compze.Tessaging.TessageTypes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
// ReSharper disable MemberCanBeInternal serialization...

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>An exactly-once tommand carrying its place in the order the specification sends tommands in.</summary>
public class NumberedExactlyOnceTommand : Remotable.ExactlyOnce.Tommand
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public NumberedExactlyOnceTommand() {}
   public NumberedExactlyOnceTommand(int number) => Number = number;

   public int Number { get; set; }
}

///<summary>What the receiver's handler throws on a try the specification scripts to fail.</summary>
public class IntentionalHandlerFailureException(int tommandNumber, int tryNumber)
   : Exception($"Intentional failure of try {tryNumber} of tommand {tommandNumber}")
{
   public int TommandNumber { get; } = tommandNumber;
}
