using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

///<summary>A client to one specific API endpoint</summary>
interface IRemoteApiEndpointClient
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> typermediaTommand);
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
}
