using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

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
