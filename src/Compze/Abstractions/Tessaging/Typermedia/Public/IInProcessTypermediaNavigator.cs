using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Typermedia.Public;

///<summary>Dispatches tessages within a process.</summary>
public interface IInProcessTypermediaNavigator
{
   ///<summary>Synchronously executes local handler for <paramref name="tuery"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   void Execute(IStrictlyLocalTommand tommand);
}