using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITypermediaHandlerRegistrar
{
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>;
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>;
}
