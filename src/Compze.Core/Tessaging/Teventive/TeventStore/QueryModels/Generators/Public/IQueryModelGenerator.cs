using Compze.Core.Public;
using Compze.Utilities.Functional;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

#pragma warning disable CA1040 //avoid empty interfaces
public interface IQueryModelGenerator;
#pragma warning restore CA1040 //avoid empty interfaces

public interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
{
   Option<TDocument> TryGenerate(EntityId id);
}