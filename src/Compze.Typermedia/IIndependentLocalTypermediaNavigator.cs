using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia;

///<summary>Navigates the local typermedia API — the API this endpoint itself serves — from code that runs outside any unit of<br/>
/// work: application code with no ambient scope or transaction. Each tommand executes as its own independent unit of work;<br/>
/// each tuery executes in its own fresh scope with no transaction — a tuery changes nothing, so there is nothing to commit.<br/>
/// The independent counterpart of <see cref="ISessionLocalTypermediaNavigator"/>, which navigates within the caller's<br/>
/// session.</summary>
///<remarks>Independence is asserted, not assumed: navigating from within an ambient transaction throws, because the execution<br/>
/// would silently join that transaction instead of standing alone. Inside a unit of work, navigate through<br/>
/// <see cref="ISessionLocalTypermediaNavigator"/>, which deliberately joins it. Resolvable from the root: a singleton, so<br/>
/// plain application classes take it as an ordinary constructor dependency.</remarks>
public interface IIndependentLocalTypermediaNavigator
{
   ///<summary>Executes the local handler for <paramref name="tuery"/> in its own fresh scope, with no transaction —<br/>
   /// see <see cref="ISessionLocalTypermediaNavigator.Execute{TTuery,TResult}"/>.</summary>
   TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work, committed when the call<br/>
   /// returns — see <see cref="ISessionLocalTypermediaNavigator.Execute{TResult}(IStrictlyLocalTommand{TResult})"/>.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Executes the local handler for <paramref name="tommand"/> as its own unit of work, committed when the call<br/>
   /// returns — see <see cref="ISessionLocalTypermediaNavigator.Execute(IStrictlyLocalTommand)"/>.</summary>
   void Execute(IStrictlyLocalTommand tommand);
}
