using Compze.Tessaging._private.Transport;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.TessageBus._private.Inbox;

///<summary>The receiving half of the endpoint's exactly-once Tessaging pipeline: arriving tessages are registered here by the<br/>
/// endpoint's transport request handling, persisted and deduped, then handled transactionally, one at a time in the order<br/>
/// they arrived: a tessage whose handling fails is tried again for as long as the endpoint's<br/>
/// <see cref="RetryPolicy"/> allow, then marked failed.</summary>
interface IInbox
{
   Task StartAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);

   ///<summary>Waits (best-effort) for every already-received tessage to finish handling, so the endpoint tears down with empty<br/>
   /// queues, holding no thread while it waits, and then stops handling: this process handles nothing after it returns. A<br/>
   /// tessage that waits to be tried again, and those behind it, are left for the recovery scan at the endpoint's next start.<br/>
   /// Called during shutdown after the transport has stopped, so nothing new arrives.</summary>
   Task DrainAndStopHandlingAsync();
}
