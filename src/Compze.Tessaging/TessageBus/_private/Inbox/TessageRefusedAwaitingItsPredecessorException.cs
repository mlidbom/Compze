using Compze.Contracts;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

///<summary>The inbox refused an arriving exactly-once tessage because it is ahead of its pair's delivery stream: its<br/>
/// predecessor has not been admitted yet (see <c>ITessagingSqlLayer.IInboxSqlLayer.SaveTessageAsync</c>). Travels back over<br/>
/// the transport as the delivery's failure, so the sender's retry redelivers — and since the sender's exactly-once send queue<br/>
/// is ordered by delivery stream sequence number, the retry leads with the missing predecessor and the stream heals itself.</summary>
///<remarks>A transient ordering condition, not a bug: it arises when the sender's queue is momentarily ahead of the stream —<br/>
/// most plainly a commit-hook enqueue racing the recovery backlog load after a restart. Refusing at admission is what makes<br/>
/// in-order admission hold by construction instead of trusting every sender-side path to enqueue in order.</remarks>
class TessageRefusedAwaitingItsPredecessorException : Exception
{
   internal TessageRefusedAwaitingItsPredecessorException(TransportTessage.InComing tessage)
      : base($"The inbox refused tessage {tessage.TessageId} at {tessage.DeliveryStreamPosition._assert().NotNull()}: " +
             "its predecessor in the stream has not been admitted yet. The sender's sequence-ordered retry will redeliver the stream in order.") {}
}
