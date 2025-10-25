using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteApiClient
{
   Task<TResult> TueryAsync<TResult>(IRemotableTuery<TResult> tuery);
   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> tommand);
   Task PostAsync(IAtMostOnceHypermediaTommand tommand);
}
