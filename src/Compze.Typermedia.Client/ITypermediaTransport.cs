using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Typermedia.Client;

interface ITypermediaTransport
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndpointAddress address);
   Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address);
}
