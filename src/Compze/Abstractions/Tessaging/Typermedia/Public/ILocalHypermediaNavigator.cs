using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Typermedia.Public;

///<summary>Dispatches tessages within a process.</summary>
public interface IInProcessHypermediaNavigator
{
   ///<summary>Synchronously executes local handler for <paramref name="tuery"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand);

   ///<summary>Synchronously executes local handler for <paramref name="tommand"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
   void Execute(IStrictlyLocalTommand tommand);
}