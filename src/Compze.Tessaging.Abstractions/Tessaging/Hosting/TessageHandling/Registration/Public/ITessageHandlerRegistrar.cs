using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent;
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand;
}
