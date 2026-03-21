using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object> GetTommandHandler(ITommand tessage);

    Action<ITommand> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent>> GetTeventHandlers(Type teventType);

    ITeventDispatcher<ITevent> CreateTeventDispatcher();

    ISet<StructuralTypeId> HandledRemoteTessageTypeIds();
}