using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

#pragma warning disable CA1040 //avoid empty interfaces
public interface IQueryModelGenerator;
#pragma warning restore CA1040 //avoid empty interfaces

public interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
{
   TDocument? TryGenerate(EntityId id);
}