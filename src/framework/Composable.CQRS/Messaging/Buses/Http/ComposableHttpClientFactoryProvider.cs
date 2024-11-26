using System;
using System.Net.Http;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Http;

interface IComposableHttpClientFactoryProvider
{
   Task<TResult> UseAsync<TResult>(Func<HttpClient, Task<TResult>> action);

   async Task<TResult> QueryAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IRemotableQuery<TResult> query, IRemotableMessageSerializer serializer)
   {
      return await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add("MessageId", message.Id.ToString());
         content.Headers.Add("PayloadTypeId", message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}/internal/rpc/query");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {query.GetType().FullName}
                                                         Body:
                                                         {message.Body}
                                                         """);
         }

         var resultJson = await response.Content.ReadAsStringAsync().CaF();
         var result = (TResult)serializer.DeserializeResponse(typeof(TResult), resultJson);
         return result;
      }).CaF();
   }

   async Task<TResult> PostAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceCommand<TResult> query, IRemotableMessageSerializer serializer)
   {
      return await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add("MessageId", message.Id.ToString());
         content.Headers.Add("PayloadTypeId", message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}/internal/rpc/command-with-result");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {query.GetType().FullName}
                                                         Body:
                                                         {message.Body}
                                                         """);
         }

         var resultJson = await response.Content.ReadAsStringAsync().CaF();
         var result = (TResult)serializer.DeserializeResponse(typeof(TResult), resultJson);
         return result;
      }).CaF();
   }
}

class ComposableHttpClientFactoryProvider : IComposableHttpClientFactoryProvider
{
   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };

   // ReSharper disable once MemberCanBeMadeStatic.Local
   HttpClient CreateClient() => new(Handler, disposeHandler: false);

   public async Task<TResult> UseAsync<TResult>(Func<HttpClient, Task<TResult>> action)
   {
      using var client = CreateClient();
      return await action(client).CaF();
   }
}
