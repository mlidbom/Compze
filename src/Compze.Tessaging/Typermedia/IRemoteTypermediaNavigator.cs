using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Typermedia;

///<summary>Navigates remote typermedia APIs — the APIs other endpoints serve: posts at-most-once typermedia tommands to them<br/>
/// and gets remotable tueries' results from them. The remote sibling of <see cref="ILocalTypermediaNavigatorSession"/> and<br/>
/// <see cref="IIndependentLocalTypermediaNavigator"/>, which navigate this endpoint's own API.</summary>
///<remarks>Independent by nature, with no unit-of-work flavor to pair with: a typermedia tessage cannot be sent remotely from<br/>
/// within a transaction (<see cref="ICannotBeSentRemotelyFromWithinTransaction"/>, asserted on every call), so remote<br/>
/// navigation never has a caller's unit of work to join. A singleton, resolvable from the root — plain application classes<br/>
/// take it as an ordinary constructor dependency.</remarks>
public interface IRemoteTypermediaNavigator
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   void Post(IAtMostOnceTypermediaTommand tommand);

   Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand);
   TResult Post<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand);

   ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="tuery"/></summary>
   Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery);

   ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
   TResult Get<TResult>(IRemotableTuery<TResult> tuery);
}