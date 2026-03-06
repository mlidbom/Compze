using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia;

public interface IRemoteTypermediaNavigator
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   void Post(IAtMostOnceTypermediaTommand tommand);

   Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> typermediaTommand);
   TResult Post<TResult>(IAtMostOnceTommand<TResult> typermediaTommand);

   ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="tuery"/></summary>
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery);

   ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
   TResult Get<TResult>(IRemotableTuery<TResult> tuery);
}