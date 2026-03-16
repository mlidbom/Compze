using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IServiceLocatorKernel> handler) where TTevent : ITevent;
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IServiceLocatorKernel> handler) where TTommand : ITommand;
}
