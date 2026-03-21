using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistry
{
   Action<ITommand> GetVoidTommandHandler(ITommand tommand);
   Func<ITommand, object> GetTommandHandlerWithReturnValue(Type tommandType);
   Func<ITuery<object>, object> GetTueryHandler(Type tueryType);

   Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
   Func<ITommand<TResult>, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

   ISet<MappedTypeId> HandledRemoteTypermediaTypeIds();
}
