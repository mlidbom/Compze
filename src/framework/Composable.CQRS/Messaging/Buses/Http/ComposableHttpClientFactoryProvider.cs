using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Composable.Functional;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;
using Composable.SystemCE;
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
            var problemDetails = (await response.Content.ReadFromJsonAsync<ProblemDetails>().CaF()).NotNull();

            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {query.GetType().FullName}
                                                         Body:
                                                         {message.Body}

                                                         Exception Type: {problemDetails.Type}
                                                         Exception Message: {problemDetails.Detail}
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
            var problemDetails = (await response.Content.ReadFromJsonAsync<ProblemDetails>().CaF()).NotNull();

            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {query.GetType().FullName}
                                                         Body:
                                                         {message.Body}

                                                         Exception Type: {problemDetails.Type}
                                                         Exception Message: {problemDetails.Detail}
                                                         """);
         }

         var resultJson = await response.Content.ReadAsStringAsync().CaF();
         var result = (TResult)serializer.DeserializeResponse(typeof(TResult), resultJson);
         return result;
      }).CaF();
   }

   async Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceHypermediaCommand query, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add("MessageId", message.Id.ToString());
         content.Headers.Add("PayloadTypeId", message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}/internal/rpc/command-no-result");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = (await response.Content.ReadFromJsonAsync<ProblemDetails>().CaF()).NotNull();

            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {query.GetType().FullName}
                                                         Body:
                                                         {message.Body}

                                                         Exception Type: {problemDetails.Type}
                                                         Exception Message: {problemDetails.Detail}
                                                         """);
         }

         await response.Content.ReadAsStringAsync().CaF();
         return Unit.Instance;
      }).CaF();
   }
}

public class ProblemDetails
{
   public string Type { get; set; }
   public string Title { get; set; }
   public int Status { get; set; }
   public string Detail { get; set; }
   public string Instance { get; set; }
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
