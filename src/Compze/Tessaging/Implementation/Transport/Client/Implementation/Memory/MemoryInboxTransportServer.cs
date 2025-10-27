using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class MemoryInboxTransportServerRegistrar
{
   internal static IComponentRegistrar MemoryTransport(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IInboxTransportServer, MemoryInboxTransportServer>()
                                  .CreatedBy((EndpointId endpointId, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, IServiceLocator serviceLocator) => new MemoryInboxTransportServer(endpointId, serviceLocator)));
}

class MemoryInboxTransportServer : IInboxTransportServer
{
   readonly LazyCE<IInbox> _inbox;
   readonly LazyCE<Inbox.HandlerExecutionEngine> _engine;

   internal MemoryInboxTransportServer(EndpointId endpointId, IServiceLocator serviceLocator)
   {
      _inbox = new LazyCE<IInbox>(serviceLocator.Resolve<IInbox>);
      _engine = new LazyCE<Inbox.HandlerExecutionEngine>(serviceLocator.Resolve<Inbox.HandlerExecutionEngine>);;
      Address = new Uri($"memory://{endpointId.GuidValue.ToString()}");
   }

   public Uri Address { get; }

   public bool Running { get; private set; }
   public ValueTask DisposeAsync() => ValueTask.CompletedTask;

   public Task StartAsync()
   {
      Assert.State.Is(!Running);
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
            case TransportTessage.TransportTessageType.AtMostOnceTommandWithReturnValue:
               return (await _inbox.Value.Receive(incomingTessage).caf())
                     .NotNull()
                     .CastTo<TResult>();
            case TransportTessage.TransportTessageType.NonTransactionalTuery:
               return (await _engine.Value.Enqueue(incomingTessage).caf())
                     .NotNull()
                     .CastTo<TResult>();
            case TransportTessage.TransportTessageType.ExactlyOnceTevent:
            case TransportTessage.TransportTessageType.AtMostOnceTommand:
            case TransportTessage.TransportTessageType.ExactlyOnceTommand:
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
            case TransportTessage.TransportTessageType.ExactlyOnceTevent:
            case TransportTessage.TransportTessageType.AtMostOnceTommand:
            case TransportTessage.TransportTessageType.ExactlyOnceTommand:
               await _inbox.Value.Receive(incomingTessage).caf();
               return;
            case TransportTessage.TransportTessageType.AtMostOnceTommandWithReturnValue:
            case TransportTessage.TransportTessageType.NonTransactionalTuery:
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         throw new TessageDispatchingFailedException(ex.ToString());
      }
   }
}
