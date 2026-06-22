using Compze.Abstractions.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

public interface IQueryModelReader
{
   TValue Get<TValue>(EntityId key) where TValue : class;
}

public interface IVersioningQueryModelReader : IQueryModelReader
{
   TValue GetVersion<TValue>(EntityId key, int version) where TValue : class;
}
