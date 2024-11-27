using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Composable.Functional;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Buses.Implementation.Http;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

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
         var requestUri = new Uri($"{address.AspNetAddress}{Routes.Rpc.Query}");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = await ProblemDetails.FromResponse(response).CaF();

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
         var requestUri = new Uri($"{address.AspNetAddress}{Routes.Rpc.CommandWithResult}");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = await ProblemDetails.FromResponse(response).CaF();

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
         var requestUri = new Uri($"{address.AspNetAddress}{Routes.Rpc.CommandNoResult}");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = await ProblemDetails.FromResponse(response).CaF();

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

   async Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceCommand command, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add("MessageId", message.Id.ToString());
         content.Headers.Add("PayloadTypeId", message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{Routes.Messaging.Command}");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = await ProblemDetails.FromResponse(response).CaF();

            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {command.GetType().FullName}
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

   async Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceEvent @event, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add("MessageId", message.Id.ToString());
         content.Headers.Add("PayloadTypeId", message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{Routes.Messaging.Event}");
         var response = await it.PostAsync(requestUri, content).CaF();
         if(!response.IsSuccessStatusCode)
         {
            var problemDetails = await ProblemDetails.FromResponse(response).CaF();

            throw new MessageDispatchingFailedException($"""
                                                         Uri:        {requestUri}
                                                         StatusCode: {response.StatusCode}
                                                         Type:       {@event.GetType().FullName}
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

[UsedImplicitly]class ProblemDetails
{
   public string Type { get; set; } = "";
   public string Title { get; set; } = "";
   public int Status { get; set; }
   public string Detail { get; set; } = "";
   public string Instance { get; set; } = "";

   internal static async Task<ProblemDetails> FromResponse(HttpResponseMessage response)
   {
      try
      {
         return (await response.Content.ReadFromJsonAsync<ProblemDetails>().CaF()).NotNull();
      }catch(Exception)
      {
         throw new FailedToExtractProblemDetailsException(response);
      }
   }
}

class FailedToExtractProblemDetailsException : Exception
{
   public FailedToExtractProblemDetailsException(HttpResponseMessage response) : base($"""
                                                                                       Failed to extract problem details from response.
                                                                                       RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                       Status code: {response.StatusCode}
                                                                                       Reason: {response.ReasonPhrase}
                                                                                       """)
   {
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
