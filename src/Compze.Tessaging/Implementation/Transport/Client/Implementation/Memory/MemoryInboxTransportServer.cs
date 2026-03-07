using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Typermedia.Hosting;

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
   readonly LazyCE<TypermediaHandlerExecutor> _typermediaExecutor;
   static readonly Dictionary<EndpointId, int> EndpointIdToInstanceCounts = new();

   public MemoryInboxTransportServer(EndpointId endpointId, IServiceLocator serviceLocator, IRemotableTessageSerializer serializer)
   {
      _serializer = serializer;
      _inbox = new LazyCE<IInbox>(serviceLocator.Resolve<IInbox>);
      _typermediaExecutor = new LazyCE<TypermediaHandlerExecutor>(serviceLocator.Resolve<TypermediaHandlerExecutor>);

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

         var tessage = incomingTessage.DeserializeTessageAndCacheForNextCall();

         switch(incomingTessage.TessageTypeEnum)
         {
            case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
               return await Task.Run(() => RoundTripSerialize((TResult)_typermediaExecutor.Value.ExecuteTommandWithResult(tessage))).caf();
            case TransportTessageType.TyperMediaTuery:
               return await Task.Run(() => RoundTripSerialize((TResult)_typermediaExecutor.Value.ExecuteTuery(tessage))).caf();
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
               var tessage = incomingTessage.DeserializeTessageAndCacheForNextCall();
               await Task.Run(() => _typermediaExecutor.Value.ExecuteVoidTommand((IAtMostOnceTypermediaTommand)tessage)).caf();
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
