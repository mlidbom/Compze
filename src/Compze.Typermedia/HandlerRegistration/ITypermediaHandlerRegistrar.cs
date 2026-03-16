using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistrar
{
   ITypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IScopeServiceLocator> handler) where TTommand : ITommand;
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IScopeServiceLocator, TResult> handler) where TTommand : ITommand<TResult>;
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeServiceLocator, TResult> handler) where TTuery : ITuery<TResult>;
}
