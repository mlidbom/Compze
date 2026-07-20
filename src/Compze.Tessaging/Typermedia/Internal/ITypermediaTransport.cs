using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Typermedia.Internal;

interface ITypermediaTransport
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> command, EndpointAddress address);
   Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address);
}
