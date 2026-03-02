using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

public interface IQueryModelReader
{
   TValue Get<TValue>(EntityId key);
}

public interface IVersioningQueryModelReader : IQueryModelReader
{
   TValue GetVersion<TValue>(EntityId key, int version);
}
