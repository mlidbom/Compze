using System.Collections.Concurrent;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Infrastructure;

static class InMemoryInfrastructureNetwork
{
   static readonly ConcurrentDictionary<EndPointAddress, InfrastructureQueryExecutor> Executors = new();

   public static void BindExecutor(EndPointAddress address, InfrastructureQueryExecutor executor) => Executors[address] = executor;

   public static void UnBind(EndPointAddress address) => Executors.TryRemove(address, out _);

   public static InfrastructureQueryExecutor GetExecutor(EndPointAddress address) => Executors[address];
}
