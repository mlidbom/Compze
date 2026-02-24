using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

#pragma warning disable CA1040 // Marker interface used for type-routing
public interface IVersioningQueryModelGenerator : IQueryModelGenerator;

public interface IVersioningQueryModelGenerator<TDocument> : IQueryModelGenerator<TDocument>
{
   TDocument? TryGenerate(EntityId id, int version);
}