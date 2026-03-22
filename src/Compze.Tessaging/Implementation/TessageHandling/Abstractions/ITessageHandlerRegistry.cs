using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IScopeResolver> GetTommandHandler(ITommand tessage);

    Action<ITommand, IScopeResolver> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type teventType);

    void DispatchTevent(ITevent tevent, IScopeResolver scopeResolver);

    ISet<StructuralTypeId> HandledRemoteTessageTypeIds();
}