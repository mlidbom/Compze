using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistry
{
   Action<ITommand, IScopeServiceLocator> GetVoidTommandHandler(ITommand tommand);
   Func<ITommand, IScopeServiceLocator, object> GetTommandHandlerWithReturnValue(Type tommandType);
   Func<ITuery<object>, IScopeServiceLocator, object> GetTueryHandler(Type tueryType);

   Func<IStrictlyLocalTuery<TTuery, TResult>, IScopeServiceLocator, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
   Func<ITommand<TResult>, IScopeServiceLocator, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

   ISet<TypeId> HandledRemoteTypermediaTypeIds();
}
