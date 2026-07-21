using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

public interface IQueryModelReader
{
   TValue Get<TValue>(EntityId key) where TValue : class;
}

public interface IVersioningQueryModelReader : IQueryModelReader
{
   TValue GetVersion<TValue>(EntityId key, int version) where TValue : class;
}
