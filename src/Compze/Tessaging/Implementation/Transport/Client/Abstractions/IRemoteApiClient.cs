using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

///<summary>A client to one specific remote API</summary>
interface IRemoteApiClient
{
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> tommand);
   Task PostAsync(IAtMostOnceHypermediaTommand tommand);
}
