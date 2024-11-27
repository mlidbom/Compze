﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;

namespace Composable.Messaging.Buses.Http;

interface IHttpApiClient
{
   Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, IRemotableMessageSerializer serializer, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
}
