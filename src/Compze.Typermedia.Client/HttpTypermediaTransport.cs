using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Typermedia.Client;

public static class HttpTypermediaTransportRegistrar
{
   ///<summary>Registers the HTTP implementation of the Typermedia client transport, plus the HTTP endpoint-discovery query transport<br/>
   /// and the <see cref="IHttpClientFactoryCE"/> both post through (shared with every other HTTP<br/>
   /// communication style, so registered only if nothing else did yet).</summary>
   public static IComponentRegistrar HttpTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.HttpClientFactoryCEIfNotRegistered()
                  .HttpEndpointDiscoveryQueryTransportIfNotRegistered()
                  .Register(Client.HttpTypermediaTransport.RegisterWith);
}

class HttpTypermediaTransport : ITypermediaTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaTransport>()
                                     .CreatedBy((IHttpClientFactoryCE factory, ITypermediaSerializer serializer, ITypeMap typeMap) => new HttpTypermediaTransport(factory, serializer, typeMap)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly ITypermediaSerializer _serializer;
   readonly ITypeMap _typeMap;

   HttpTypermediaTransport(IHttpClientFactoryCE httpClientFactory, ITypermediaSerializer serializer, ITypeMap typeMap)
   {
      _httpClientFactory = httpClientFactory;
      _serializer = serializer;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address)
      => await PostInternalWithResult<TResult>(tuery, address, HttpConstants.Routes.Typermedia.Tuery).caf();

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndpointAddress address)
      => await PostInternalWithResult<TResult>(command, address, HttpConstants.Routes.Typermedia.TommandWithResult).caf();

   public async Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address)
      => await PostInternalNoResult(command, address, HttpConstants.Routes.Typermedia.TommandNoResult).caf();

   async Task<TResult> PostInternalWithResult<TResult>(ITypermediaTessage tessage, EndpointAddress address, string route)
   {
      var response = await PostToEndpoint(tessage, address, route).caf();
      var resultJson = await response.Content.ReadAsStringAsync().caf();
      return _serializer.DeserializeResult<TResult>(resultJson);
   }

   async Task PostInternalNoResult(ITypermediaTessage tessage, EndpointAddress address, string route)
      => await PostToEndpoint(tessage, address, route).caf();

   async Task<HttpResponseMessage> PostToEndpoint(ITypermediaTessage tessage, EndpointAddress address, string route)
   {
      var requestUri = new Uri(address.Uri, route);
      var body = _serializer.SerializeTessage(tessage);
      var typeId = _typeMap.GetId(tessage.GetType());

      using var httpClient = _httpClientFactory.CreateClient();
      using var content = new StringContent(body);
      content.Headers.Add(HttpConstants.Headers.TessageId, ((tessage as IAtMostOnceTessage)?.Id ?? new TessageId()).ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, typeId.CanonicalString);

      var response = await httpClient.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new MessageDispatchingFailedException($"""
                                                      Uri:        {requestUri}
                                                      StatusCode: {response.StatusCode}
                                                      Type:       {tessage.GetType().FullName}
                                                      Body:
                                                      {body}

                                                      Exception Type: {problemDetails.Type}
                                                      Exception Tessage: {problemDetails.Detail}
                                                      """);
      }

      return response;
   }
}
