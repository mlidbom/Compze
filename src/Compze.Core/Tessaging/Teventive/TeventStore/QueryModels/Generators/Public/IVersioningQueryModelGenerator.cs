using Compze.Core.Public;
using Compze.Utilities.Functional;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

public interface IVersioningQueryModelGenerator : IQueryModelGenerator;

public interface IVersioningQueryModelGenerator<TDocument> : IQueryModelGenerator<TDocument>
{
   Option<TDocument> TryGenerate(EntityId id, int version);
}