using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>What <see cref="IPeerAdministration.Decommission"/> did: the peer that left the endpoint's memory, and everything the<br/>
/// endpoint still held for it that was discarded as part of the act — decommissioning a peer with undelivered tessages is loud<br/>
/// and deliberate, so the act itself reports what it discarded, never discarding as a silent side effect<br/>
/// (see <c>dev_docs/TODO/WIP/Tessaging/durable-peer-topology.md</c>).</summary>
public class PeerDecommissionReport
{
   internal PeerDecommissionReport(EndpointId decommissionedPeer, IReadOnlyList<DiscardedTessages> discarded)
   {
      DecommissionedPeer = decommissionedPeer;
      Discarded = discarded;
   }

   ///<summary>The peer that was decommissioned.</summary>
   public EndpointId DecommissionedPeer { get; }

   ///<summary>Everything the endpoint held for the peer, discarded by the act — one entry per kind of held tessages, empty when<br/>
   /// nothing was owed. A zero-count entry is meaningful: it names a hold that ended with nothing in it, such as a required<br/>
   /// peer's first-contact hold.</summary>
   public IReadOnlyList<DiscardedTessages> Discarded { get; }

   ///<summary>One kind of tessages the decommission discarded: what they were, in words, and how many there were.</summary>
   public class DiscardedTessages
   {
      internal DiscardedTessages(string description, int count)
      {
         Description = description;
         Count = count;
      }

      ///<summary>What was discarded, in words — e.g. undelivered exactly-once tessages awaiting the peer's return, stranded<br/>
      /// tommands, queued best-effort tevents — including the discarded types where the tier knows them.</summary>
      public string Description { get; }

      ///<summary>How many tessages of this kind were discarded.</summary>
      public int Count { get; }
   }
}
