using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Typermedia.Client;

interface ITypermediaTransport
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> command, EndpointAddress address);
   Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address);
}
