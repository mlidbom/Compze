using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistry
{
   Action<ITommand, IUnitOfWorkResolver> GetVoidTommandHandler(ITommand tommand);
   Func<ITommand, IUnitOfWorkResolver, object> GetTommandHandlerWithReturnValue(Type tommandType);
   Func<ITuery<object>, IScopeResolver, object> GetTueryHandler(Type tueryType);

   Func<IStrictlyLocalTuery<TTuery, TResult>, IScopeResolver, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
   Func<ITommand<TResult>, IUnitOfWorkResolver, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

   ISet<TypeId> HandledRemoteTypermediaTypeIds();
}
