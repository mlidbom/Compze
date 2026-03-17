using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler) where TTevent : ITevent;
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IScopeResolver> handler) where TTommand : ITommand;
}
