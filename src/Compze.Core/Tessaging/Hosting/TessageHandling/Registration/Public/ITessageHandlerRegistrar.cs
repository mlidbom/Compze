using Compze.Abstractions.Tessaging.Public;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent;
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand;
    ITessageHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>;
    ITessageHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>;
}
