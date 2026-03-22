using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistry
{
   Action<ITommand, IScopeResolver> GetVoidTommandHandler(ITommand tommand);
   Func<ITommand, IScopeResolver, object> GetTommandHandlerWithReturnValue(Type tommandType);
   Func<ITuery<object>, IScopeResolver, object> GetTueryHandler(Type tueryType);

   Func<IStrictlyLocalTuery<TTuery, TResult>, IScopeResolver, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
   Func<ITommand<TResult>, IScopeResolver, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

   ISet<StructuralTypeId> HandledRemoteTypermediaTypeIds();
}
