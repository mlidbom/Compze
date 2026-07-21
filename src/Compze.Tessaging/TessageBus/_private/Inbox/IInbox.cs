using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

///<summary>The receiving half of the endpoint's exactly-once Tessaging pipeline: arriving tessages are registered here by the<br/>
/// endpoint's transport request handling, persisted and deduped, then handled transactionally — retried until handled.</summary>
interface IInbox
{
   Task StartAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);

   ///<summary>Waits (best-effort) for every already-received tessage to finish handling, so the endpoint tears down with empty<br/>
   /// queues. Called during shutdown after the transport has stopped, so nothing new arrives and the inbox drains to idle.</summary>
   void AwaitAllReceivedTessagesProcessed();
}
