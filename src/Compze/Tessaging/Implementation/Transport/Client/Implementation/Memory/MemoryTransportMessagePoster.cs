using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class MemoryTransportMessagePosterApiTransportClientRegistrar
{
   internal static IComponentRegistrar MemoryApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(MemoryTransportMessagePoster.RegisterWith);
}

class MemoryTransportMessagePoster : ITransportMessagePoster
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IRemotableTessageSerializer serializer) => new MemoryTransportMessagePoster(serializer)));

   readonly IRemotableTessageSerializer _serializer;

   MemoryTransportMessagePoster(IRemotableTessageSerializer serializer) =>
      _serializer = serializer;

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri)
   {
      var response = await PostAsyncInternal(tessage, realTessage, requestUri).caf();

      var resultJson = await response.Content.ReadAsStringAsync().caf();
      var result = _serializer.DeserializeResponse<TResult>(resultJson);
      return result;
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri) =>
      await PostAsyncInternal(tessage, realTessage, requestUri).caf();

   async Task<HttpResponseMessage> PostAsyncInternal(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri) =>
      throw new NotImplementedException("");
}
