using Compze.Core.Public;
using Compze.Utilities.Functional;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

#pragma warning disable CA1040 // Marker interface used for type-routing
public interface IVersioningQueryModelGenerator : IQueryModelGenerator;

public interface IVersioningQueryModelGenerator<TDocument> : IQueryModelGenerator<TDocument>
{
   Option<TDocument> TryGenerate(EntityId id, int version);
}