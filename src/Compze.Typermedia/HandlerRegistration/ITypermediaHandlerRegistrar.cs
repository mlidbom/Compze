using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistrar
{
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>;
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>;
}
