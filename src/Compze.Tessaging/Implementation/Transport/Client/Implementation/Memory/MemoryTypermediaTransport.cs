using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryTypermediaTransportRegistrar
{
   public static IComponentRegistrar MemoryTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Memory.MemoryTypermediaTransport.RegisterWith);
}

class MemoryTypermediaTransport : ITypermediaTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaTransport>()
                                     .CreatedBy((IRemotableTessageSerializer serializer) => new MemoryTypermediaTransport(serializer)));

   readonly IRemotableTessageSerializer _serializer;

   MemoryTypermediaTransport(IRemotableTessageSerializer serializer) => _serializer = serializer;

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndPointAddress address)
   {
      try
      {
         var executor = InMemoryTypermediaNetwork.GetExecutor(address);
         return await Task.Run(() => RoundTripSerialize((TResult)executor.ExecuteTuery(tuery))).caf();
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch tuery {tuery.GetType().FullName}");
         throw new MessageDispatchingFailedException(ex.ToString());
      }
   }

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndPointAddress address)
   {
      try
      {
         var executor = InMemoryTypermediaNetwork.GetExecutor(address);
         return await Task.Run(() => RoundTripSerialize((TResult)executor.ExecuteTommandWithResult(command))).caf();
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch command {command.GetType().FullName}");
         throw new MessageDispatchingFailedException(ex.ToString());
      }
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand command, EndPointAddress address)
   {
      try
      {
         var executor = InMemoryTypermediaNetwork.GetExecutor(address);
         await Task.Run(() => executor.ExecuteVoidTommand(command)).caf();
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch command {command.GetType().FullName}");
         throw new MessageDispatchingFailedException(ex.ToString());
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
