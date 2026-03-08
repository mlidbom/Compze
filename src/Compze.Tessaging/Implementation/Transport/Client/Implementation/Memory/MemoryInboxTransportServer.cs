using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.Transport;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryInboxTransportServerRegistrar
{
   public static IComponentRegistrar MemoryTransport(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IInboxTransportServer, MemoryInboxTransportServer>()
                                  .CreatedBy((EndpointId endpointId, IServiceLocator serviceLocator) => new MemoryInboxTransportServer(endpointId, serviceLocator)));
}

class MemoryInboxTransportServer : IInboxTransportServer
{
   readonly LazyCE<IInbox> _inbox;
   static readonly Dictionary<EndpointId, int> EndpointIdToInstanceCounts = new();

   public MemoryInboxTransportServer(EndpointId endpointId, IServiceLocator serviceLocator)
   {
      _inbox = new LazyCE<IInbox>(serviceLocator.Resolve<IInbox>);

      lock(EndpointIdToInstanceCounts)
      {
         var instanceCount = EndpointIdToInstanceCounts[endpointId] = EndpointIdToInstanceCounts.GetOrAdd(endpointId, () => 0) + 1;
         Address = new Uri($"memory://{endpointId}/instance/{instanceCount}"); //Create an address that can be helpful in the debugger.
      }
   }

   public Uri Address { get; }

   bool Running { get; set; }
   public ValueTask DisposeAsync() => ValueTask.CompletedTask;

   public Task StartAsync()
   {
      State.Assert(!Running);
      this.Log().Info($"Binding at {Address}");
      Running = true;
      var endPointAddress = new EndPointAddress(Address);
      InMemoryTransportNetwork.BindServerToAddress(endPointAddress, this);
      return Task.CompletedTask;
   }

   public Task StopAsync()
   {
      this.Log().Info($"Unbinding at {Address}");
      Running = false;
      var endPointAddress = new EndPointAddress(Address);
      InMemoryTransportNetwork.UnBindAddress(endPointAddress);
      return Task.CompletedTask;
   }

   public async Task PostAsync(TransportTessage.InComing incomingTessage)
   {
      try
      {
         this.Log().Debug($"Receiving {incomingTessage.TessageTypeEnum} tessage {incomingTessage.TessageId} at {Address}");
         if(!Running)
            throw new Exception("Transport is not running");

         switch(incomingTessage.TessageTypeEnum)
         {
            case TransportTessageType.ExactlyOnceTevent:
            case TransportTessageType.ExactlyOnceTommand:
               await _inbox.Value.ReceiveAsync(incomingTessage).caf();
               return;
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch tessage {incomingTessage.TessageId}");
         throw new MessageDispatchingFailedException(ex.ToString());
      }
   }
}
