using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia._private;

interface ITypermediaTransport
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> command, EndpointAddress address);
   Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address);
}
