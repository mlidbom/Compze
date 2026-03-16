using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IServiceLocatorKernel> GetTommandHandler(ITommand tessage);

    Action<ITommand, IServiceLocatorKernel> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IServiceLocatorKernel>> GetTeventHandlers(Type teventType);

    void DispatchTevent(ITevent tevent, IServiceLocatorKernel kernel);

    ISet<TypeId> HandledRemoteTessageTypeIds();
}