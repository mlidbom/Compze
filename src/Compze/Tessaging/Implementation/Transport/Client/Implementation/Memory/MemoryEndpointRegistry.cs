using System.Collections.Generic;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

class MemoryEndpointRegistry
{
   IList<IEndpoint> _endPoints = new List<IEndpoint>();
   public void Register(IEndpoint endpoint, IInbox inbox, Inbox.HandlerExecutionEngine execution)
   {
      _endPoints.Add(endpoint);
   }
}
