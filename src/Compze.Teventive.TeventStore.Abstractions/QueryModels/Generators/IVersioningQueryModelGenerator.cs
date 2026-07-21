using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

#pragma warning disable CA1040 // Marker interface used for type-routing

public interface IVersioningQueryModelGenerator<out TDocument> : IQueryModelGenerator<TDocument>
{
   TDocument? TryGenerate(EntityId id, int version);
}