using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistrar
{
   ITypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IServiceLocatorKernel> handler) where TTommand : ITommand;
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IServiceLocatorKernel, TResult> handler) where TTommand : ITommand<TResult>;
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IServiceLocatorKernel, TResult> handler) where TTuery : ITuery<TResult>;
}
