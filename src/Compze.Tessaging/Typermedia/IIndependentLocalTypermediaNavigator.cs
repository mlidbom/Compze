
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Navigates the local typermedia API — the API this endpoint itself serves — from code that runs outside any unit of<br/>
/// work: application code with no ambient scope or transaction. Each tommand executes as its own independent unit of work;<br/>
/// each tuery executes in its own fresh scope with no transaction — a tuery changes nothing, so there is nothing to commit.<br/>
/// The independent counterpart of <see cref="ILocalTypermediaNavigatorSession"/>, which navigates within the caller's<br/>
/// session.</summary>
///<remarks>Independence is asserted, not assumed: navigating from within an ambient transaction throws, because the execution<br/>
/// would silently join that transaction instead of standing alone. Inside a unit of work, navigate through<br/>
/// <see cref="ILocalTypermediaNavigatorSession"/>, which deliberately joins it. Resolvable from the root: a singleton, so<br/>
/// plain application classes take it as an ordinary constructor dependency.</remarks>
public interface IIndependentLocalTypermediaNavigator
{
   ///<summary>Executes the local handler for <paramref name="tuery"/> in its own fresh scope, with no transaction —<br/>
   /// see <see cref="ILocalTypermediaNavigatorSession.Execute{TTuery,TResult}"/>.</summary>
   TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work, committed when the call<br/>
   /// returns — see <see cref="ILocalTypermediaNavigatorSession.Execute{TResult}(IStrictlyLocalTommand{TResult})"/>.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work, committed when the call<br/>
   /// returns — see <see cref="ILocalTypermediaNavigatorSession.Execute(IStrictlyLocalTommand)"/>.</summary>
   void Execute(IStrictlyLocalTommand tommand);

   ///<summary>Executes the local handler for <paramref name="tuery"/> in its own fresh scope, with no transaction, as<br/>
   /// <see cref="Execute{TTuery,TResult}"/> does, holding no thread while the handler awaits — for callers that are<br/>
   /// asynchronous themselves, such as an ASP.NET Core route.</summary>
   ///<remarks>What runs synchronously beneath still holds the thread it runs on: the handler's own synchronous work, such as getting a<br/>
   /// taggregate from a taggregate store, and, for a tommand, the commit, which <see cref="System.Transactions"/> makes synchronously.</remarks>
   Task<TResult> ExecuteAsync<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work, committed before the returned<br/>
   /// task completes, as <see cref="Execute{TResult}(IStrictlyLocalTommand{TResult})"/> does, holding no thread while the handler awaits.</summary>
   ///<remarks>What still holds a thread: see <see cref="ExecuteAsync{TTuery,TResult}"/>.</remarks>
   Task<TResult> ExecuteAsync<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work — see<br/>
   /// <see cref="ExecuteAsync{TResult}(IStrictlyLocalTommand{TResult})"/>.</summary>
   Task ExecuteAsync(IStrictlyLocalTommand tommand);
}
