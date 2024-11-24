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
   async Task<TResult> Query<TResult>(EndPointAddress address, TransportMessage.OutGoing query, IRemotableMessageSerializer serializer)
   {
      return await UseAsync(async it =>
      {
         var content = new StringContent(query.Body);
         content.Headers.Add("MessageId", query.Id.ToString());
         content.Headers.Add("PayloadTypeId", query.Type.GuidValue.ToString());
         var response = await it.PostAsync(new Uri($"{address.AspNetAddress}/internal/rpc/query"), content).NoMarshalling();
         if(!response.IsSuccessStatusCode)
         {
            throw new MessageDispatchingFailedException($"Query failed with status code {response.StatusCode}");
         }

         var resultJson = await response.Content.ReadAsStringAsync().NoMarshalling();
         var result = (TResult)serializer.DeserializeResponse(typeof(TResult), resultJson);
         return result;
      }).NoMarshalling();
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
      return await action(client).NoMarshalling();
   }
}
