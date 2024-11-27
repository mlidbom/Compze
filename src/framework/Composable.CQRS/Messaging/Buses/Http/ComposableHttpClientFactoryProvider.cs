using System;
using System.Net.Http;
using System.Threading.Tasks;
using Composable.Functional;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Buses.Implementation.Http;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Http;

class ComposableHttpClientFactoryProvider : IRpcClient, IMessageSender
{
   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };

   public async Task<TResult> QueryAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IRemotableQuery<TResult> query, IRemotableMessageSerializer serializer)
   {
      return await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
         content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.Query}");
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

   public async Task<TResult> PostAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceCommand<TResult> query, IRemotableMessageSerializer serializer)
   {
      return await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
         content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.CommandWithResult}");
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

   public async Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceHypermediaCommand query, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
         content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.CommandNoResult}");
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

   public async Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceCommand command, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
         content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Messaging.Command}");
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

   public async Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceEvent @event, IRemotableMessageSerializer serializer)
   {
      await UseAsync(async it =>
      {
         var content = new StringContent(message.Body);
         content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
         content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
         var requestUri = new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Messaging.Event}");
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

   // ReSharper disable once MemberCanBeMadeStatic.Local
   HttpClient CreateClient() => new(Handler, disposeHandler: false);

   public async Task<TResult> UseAsync<TResult>(Func<HttpClient, Task<TResult>> action)
   {
      using var client = CreateClient();
      return await action(client).CaF();
   }
}
