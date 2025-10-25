using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Typermedia.Public;

///<summary>Dispatches messages within a process.</summary>
public interface IInProcessHypermediaNavigator
{
   ///<summary>Synchronously executes local handler for <paramref name="tuery"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TQuery, TResult>(IStrictlyLocalTuery<TQuery, TResult> tuery) where TQuery : IStrictlyLocalTuery<TQuery, TResult>;

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   void Execute(IStrictlyLocalTommand tommand);
}