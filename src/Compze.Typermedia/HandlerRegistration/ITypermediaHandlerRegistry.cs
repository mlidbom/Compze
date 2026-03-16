using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistry
{
   Action<ITommand, IServiceLocatorKernel> GetVoidTommandHandler(ITommand tommand);
   Func<ITommand, IServiceLocatorKernel, object> GetTommandHandlerWithReturnValue(Type tommandType);
   Func<ITuery<object>, IServiceLocatorKernel, object> GetTueryHandler(Type tueryType);

   Func<IStrictlyLocalTuery<TTuery, TResult>, IServiceLocatorKernel, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
   Func<ITommand<TResult>, IServiceLocatorKernel, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

   ISet<TypeId> HandledRemoteTypermediaTypeIds();
}
