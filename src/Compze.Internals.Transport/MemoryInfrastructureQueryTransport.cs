using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Underscore;

namespace Compze.Internals.Transport;

public static class MemoryInfrastructureQueryTransportRegistrar
{
   public static IComponentRegistrar MemoryInfrastructureQueryTransport(this IComponentRegistrar registrar)
      => registrar.Register(MemoryInfrastructureQueryTransportImplementation.RegisterWith);
}

class MemoryInfrastructureQueryTransportImplementation : IInfrastructureQueryTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IInfrastructureQueryTransport>()
                                     .CreatedBy((IRemotableTessageSerializer serializer) => new MemoryInfrastructureQueryTransportImplementation(serializer)));

   readonly IRemotableTessageSerializer _serializer;

   MemoryInfrastructureQueryTransportImplementation(IRemotableTessageSerializer serializer) => _serializer = serializer;

   public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndPointAddress address)
   {
      try
      {
         var executor = InMemoryInfrastructureNetwork.GetExecutor(address);
         return await Task.Run(() => RoundTripSerialize((TResult)executor.ExecuteQuery(query))).caf();
      }
      catch(Exception ex)
      {
         this.Log().Warning(ex, $"Failed to dispatch infrastructure query {query.GetType().FullName}");
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
