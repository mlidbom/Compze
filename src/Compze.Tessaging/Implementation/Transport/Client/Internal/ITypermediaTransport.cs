using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITypermediaTransport
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndPointAddress address);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndPointAddress address);
   Task PostAsync(IAtMostOnceTypermediaTommand command, EndPointAddress address);
}
