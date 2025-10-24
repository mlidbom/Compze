using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Typermedia.Public;

///<summary>Dispatches messages within a process.</summary>
public interface IInProcessHypermediaNavigator
{
   ///<summary>Synchronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>;

   ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TResult>(IStrictlyLocalCommand<TResult> command);

   ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   void Execute(IStrictlyLocalCommand command);
}