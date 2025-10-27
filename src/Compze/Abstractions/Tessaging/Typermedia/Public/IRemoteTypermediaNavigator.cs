using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Typermedia.Public;

public interface IRemoteTypermediaNavigator
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   void Post(IAtMostOnceTypermediaTommand tommand);

   Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> tommand);
   TResult Post<TResult>(IAtMostOnceTypermediaTommand<TResult> tommand);

   ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="tuery"/></summary>
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery);

   ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
   TResult Get<TResult>(IRemotableTuery<TResult> tuery);
}