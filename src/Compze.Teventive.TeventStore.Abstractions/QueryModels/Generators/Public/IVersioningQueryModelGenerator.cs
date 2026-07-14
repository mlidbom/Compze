using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators.Public;

#pragma warning disable CA1040 // Marker interface used for type-routing

public interface IVersioningQueryModelGenerator<out TDocument> : IQueryModelGenerator<TDocument>
{
   TDocument? TryGenerate(EntityId id, int version);
}