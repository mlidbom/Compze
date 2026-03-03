using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Core.Serialization.Internal;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryInboxTransportServerRegistrar
{
   public static IComponentRegistrar MemoryTransport(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IInboxTransportServer, MemoryInboxTransportServer>()
                                  .CreatedBy((EndpointId endpointId, IRemotableTessageSerializer serializer, IServiceLocator serviceLocator) => new MemoryInboxTransportServer(endpointId, serviceLocator, serializer)));
}

class MemoryInboxTransportServer : IInboxTransportServer
{
   readonly IRemotableTessageSerializer _serializer;
   readonly LazyCE<IInbox> _inbox;
   readonly LazyCE<Inbox.HandlerExecutionEngine> _engine;
   static readonly Dictionary<EndpointId, int> EndpointIdToInstanceCounts = new();

   public MemoryInboxTransportServer(EndpointId endpointId, IServiceLocator serviceLocator, IRemotableTessageSerializer serializer)
   {
      _serializer = serializer;
      _inbox = new LazyCE<IInbox>(serviceLocator.Resolve<IInbox>);
      _engine = new LazyCE<Inbox.HandlerExecutionEngine>(serviceLocator.Resolve<Inbox.HandlerExecutionEngine>);

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
      InMemoryTransportNetwork.BindServerToAddress(new EndPointAddress(Address), this);
      return Task.CompletedTask;
   }

   public Task StopAsync()
   {
      this.Log().Info($"Unbinding at {Address}");
      Running = false;
      InMemoryTransportNetwork.UnBindAddress(new EndPointAddress(Address));
      return Task.CompletedTask;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.InComing incomingTessage)
   {
      try
      {
         this.Log().Debug($"Receiving {incomingTessage.TessageTypeEnum} tessage {incomingTessage.TessageId} at {Address}");
         if(!Running)
            throw new Exception("Transport is not running");

         switch(incomingTessage.TessageTypeEnum)
         {
            case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
               return (await _inbox.Value.ExecuteAsync(incomingTessage).caf())._assert().NotNull()
                                                                              .CastTo<TResult>()
                                                                              ._(RoundTripSerialize);
            case TransportTessageType.TyperMediaTuery:
               return (await _engine.Value.ExecuteAsync(incomingTessage).caf())._assert().NotNull()
                                                                               .CastTo<TResult>()
                                                                               ._(RoundTripSerialize);
            case TransportTessageType.ExactlyOnceTevent:
            case TransportTessageType.TypermediaAtMostOnceTommand:
            case TransportTessageType.ExactlyOnceTommand:
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch tessage {incomingTessage.TessageId}");
         throw new TessageDispatchingFailedException(ex.ToString());
      }
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
            case TransportTessageType.TypermediaAtMostOnceTommand:
               await _inbox.Value.ExecuteAsync(incomingTessage).caf();
               return;
            case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
            case TransportTessageType.TyperMediaTuery:
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch tessage {incomingTessage.TessageId}");
         throw new TessageDispatchingFailedException(ex.ToString());
      }
   }

   TResponse RoundTripSerialize<TResponse>(TResponse response)
   {
      // ReSharper disable once CompareNonConstrainedGenericWithNull
      if(response == null)
         throw new Exception("Null return values are not supported");

      return _serializer.SerializeResponse(response)
                        ._(it => _serializer.DeserializeResponse<TResponse>(it));
   }
}
