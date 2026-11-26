
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Navigates the local typermedia API — the API this endpoint itself serves — within the caller's session: the<br/>
/// navigator lives in the caller's scope, and handlers execute synchronously, resolving their dependencies from that scope.<br/>
/// A tuery needs only the scope; a tommand executes within the caller's unit of work — its ambient transaction, whose<br/>
/// presence is asserted, decides the handler's effects' fate. Code outside any scope navigates through<br/>
/// <see cref="IIndependentLocalTypermediaNavigator"/>, the independent counterpart. The remote sibling,<br/>
/// <see cref="IRemoteTypermediaNavigator"/>, browses other endpoints' APIs.</summary>
public interface ILocalTypermediaNavigatorSession
{
   ///<summary>Synchronously executes the local handler for <paramref name="tuery"/> in the caller's scope. A tuery demands no<br/>
   /// transaction — it changes nothing — but inside the caller's transaction its reads join that transaction's consistency,<br/>
   /// so results are consistent with the caller's own writes.</summary>
   TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Synchronously executes the local handler for <paramref name="tommand"/> within the caller's unit of work: the<br/>
   /// handler's effects commit or roll back with the caller's ambient transaction, whose presence is asserted —<br/>
   /// an <see cref="IStrictlyLocalTommand"/> must be sent transactionally.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Synchronously executes the local handler for <paramref name="tommand"/> within the caller's unit of work — see<br/>
   /// <see cref="Execute{TResult}(IStrictlyLocalTommand{TResult})"/>.</summary>
   void Execute(IStrictlyLocalTommand tommand);

   ///<summary>Executes the local handler for <paramref name="tuery"/> in the caller's scope, as<br/>
   /// <see cref="Execute{TTuery,TResult}"/> does, holding no thread while the handler awaits.</summary>
   ///<remarks>What the handler does synchronously runs on the calling thread, as it would through <see cref="Execute{TTuery,TResult}"/>:<br/>
   /// getting a taggregate from a taggregate store, for one, is synchronous.</remarks>
   Task<TResult> ExecuteAsync<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Executes the local handler for <paramref name="tommand"/> within the caller's unit of work, as<br/>
   /// <see cref="Execute{TResult}(IStrictlyLocalTommand{TResult})"/> does, holding no thread while the handler awaits. The<br/>
   /// caller's ambient transaction flows across the handler's awaits.</summary>
   ///<remarks>What the handler does synchronously runs on the calling thread: see <see cref="ExecuteAsync{TTuery,TResult}"/>.</remarks>
   Task<TResult> ExecuteAsync<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Executes the local handler for <paramref name="tommand"/> within the caller's unit of work — see<br/>
   /// <see cref="ExecuteAsync{TResult}(IStrictlyLocalTommand{TResult})"/>.</summary>
   Task ExecuteAsync(IStrictlyLocalTommand tommand);
}
