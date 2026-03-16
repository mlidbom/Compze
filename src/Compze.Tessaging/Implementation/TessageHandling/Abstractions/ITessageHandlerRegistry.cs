using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IScopeServiceLocator> GetTommandHandler(ITommand tessage);

    Action<ITommand, IScopeServiceLocator> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IScopeServiceLocator>> GetTeventHandlers(Type teventType);

    void DispatchTevent(ITevent tevent, IScopeServiceLocator scopeServiceLocator);

    ISet<TypeId> HandledRemoteTessageTypeIds();
}