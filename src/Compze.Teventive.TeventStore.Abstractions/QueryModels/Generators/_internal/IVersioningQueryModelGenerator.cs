using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators._internal;

#pragma warning disable CA1040 // Marker interface used for type-routing

interface IVersioningQueryModelGenerator<out TDocument> : IQueryModelGenerator<TDocument>
{
   TDocument? TryGenerate(EntityId id, int version);
}