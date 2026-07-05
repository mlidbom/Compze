using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IScopeResolver> GetTommandHandler(ITommand tessage);

    Action<ITommand, IScopeResolver> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type teventType);

    ISet<TypeId> HandledRemoteTessageTypeIds();
}