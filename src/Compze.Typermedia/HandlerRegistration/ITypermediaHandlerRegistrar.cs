using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistrar
{
   ITypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IScopeResolver> handler) where TTommand : ITommand;
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IScopeResolver, TResult> handler) where TTommand : ITommand<TResult>;
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler) where TTuery : ITuery<TResult>;
}
