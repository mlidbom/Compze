using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryInboxTransportServerRegistrar
{
   public static IComponentRegistrar MemoryTransport(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IInboxTransportServer, MemoryInboxTransportServer>()
                                  .CreatedBy((EndpointId endpointId, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, IServiceLocator serviceLocator) => new MemoryInboxTransportServer(endpointId, serviceLocator, serializer)));
}

public class MemoryInboxTransportServer : IInboxTransportServer
{
   readonly IRemotableTessageSerializer _serializer;
   readonly LazyCE<IInbox> _inbox;
   readonly LazyCE<Inbox.HandlerExecutionEngine> _engine;

   public MemoryInboxTransportServer(EndpointId endpointId, IServiceLocator serviceLocator, IRemotableTessageSerializer serializer)
   {
      _serializer = serializer;
      _inbox = new LazyCE<IInbox>(serviceLocator.Resolve<IInbox>);
      _engine = new LazyCE<Inbox.HandlerExecutionEngine>(serviceLocator.Resolve<Inbox.HandlerExecutionEngine>);
      ;
      Address = new Uri($"memory://{endpointId}");
   }

   public Uri Address { get; }

   public bool Running { get; private set; }
   public ValueTask DisposeAsync() => ValueTask.CompletedTask;

   public Task StartAsync()
   {
      Contract.State.Fulfills(!Running);
      Running = true;
      return Task.CompletedTask;
   }

   public Task StopAsync()
   {
      Running = false;
      return Task.CompletedTask;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.InComing incomingTessage)
   {
      try
      {
         if(!Running)
            throw new Exception("Transport is not running");

         switch(incomingTessage.TessageTypeEnum)
         {
            case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
               return (await _inbox.Value.ExecuteAsync(incomingTessage).caf())._assertNotNull()
                                                                               .CastTo<TResult>()
                                                                               ._(RoundTripSerialize);
            case TransportTessageType.TyperMediaTuery:
               return (await _engine.Value.ExecuteAsync(incomingTessage).caf())._assertNotNull()
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
         throw new TessageDispatchingFailedException(ex.ToString());
      }
   }

   public async Task PostAsync(TransportTessage.InComing incomingTessage)
   {
      try
      {
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
