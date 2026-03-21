using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Typermedia.Client;

public static class HttpTypermediaTransportRegistrar
{
   public static IComponentRegistrar HttpTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.Register(Client.HttpTypermediaTransport.RegisterWith);
}

class HttpTypermediaTransport : ITypermediaTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaTransport>()
                                     .CreatedBy((IHttpClientFactoryCE factory, IRemotableTessageSerializer serializer, IStructuralTypeMapper typeMapper) => new HttpTypermediaTransport(factory, serializer, typeMapper)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly IRemotableTessageSerializer _serializer;
   readonly IStructuralTypeMapper _typeMapper;

   HttpTypermediaTransport(IHttpClientFactoryCE httpClientFactory, IRemotableTessageSerializer serializer, IStructuralTypeMapper typeMapper)
   {
      _httpClientFactory = httpClientFactory;
      _serializer = serializer;
      _typeMapper = typeMapper;
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndPointAddress address)
      => await PostInternalWithResult<TResult>(tuery, address, HttpConstants.Routes.Typermedia.Tuery).caf();

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndPointAddress address)
      => await PostInternalWithResult<TResult>(command, address, HttpConstants.Routes.Typermedia.TommandWithResult).caf();

   public async Task PostAsync(IAtMostOnceTypermediaTommand command, EndPointAddress address)
      => await PostInternalNoResult(command, address, HttpConstants.Routes.Typermedia.TommandNoResult).caf();

   async Task<TResult> PostInternalWithResult<TResult>(IRemotableTessage tessage, EndPointAddress address, string route)
   {
      var response = await PostToEndpoint(tessage, address, route).caf();
      var resultJson = await response.Content.ReadAsStringAsync().caf();
      return _serializer.DeserializeResponse<TResult>(resultJson);
   }

   async Task PostInternalNoResult(IRemotableTessage tessage, EndPointAddress address, string route)
      => await PostToEndpoint(tessage, address, route).caf();

   async Task<HttpResponseMessage> PostToEndpoint(IRemotableTessage tessage, EndPointAddress address, string route)
   {
      var requestUri = new Uri(address.Uri, route);
      var body = _serializer.SerializeTessage(tessage);
      var typeId = _typeMapper.GetId(tessage.GetType());

      using var httpClient = _httpClientFactory.CreateClient();
      using var content = new StringContent(body);
      content.Headers.Add(HttpConstants.Headers.TessageId, ((tessage as IAtMostOnceTessage)?.Id ?? new TessageId()).ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, typeId.GuidValue.ToString());

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
