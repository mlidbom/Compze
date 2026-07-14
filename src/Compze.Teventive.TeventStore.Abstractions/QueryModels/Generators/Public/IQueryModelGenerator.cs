using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators.Public;

#pragma warning disable CA1040 //avoid empty interfaces
public interface IQueryModelGenerator;
#pragma warning restore CA1040 //avoid empty interfaces

public interface IQueryModelGenerator<out TDocument> : IQueryModelGenerator
{
   TDocument? TryGenerate(EntityId id);
}