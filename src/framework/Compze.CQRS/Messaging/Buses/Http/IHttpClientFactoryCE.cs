using System;
using System.Net.Http;

namespace Composable.Messaging.Buses.Http;

interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

class HttpClientFactoryCE : IHttpClientFactoryCE
{
   public HttpClient CreateClient() => new(Handler, disposeHandler: false);

   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
