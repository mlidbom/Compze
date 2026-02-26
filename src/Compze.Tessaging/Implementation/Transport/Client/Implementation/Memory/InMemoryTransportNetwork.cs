using System.Collections.Concurrent;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class InMemoryTransportNetwork
{
   static readonly ConcurrentDictionary<EndPointAddress, MemoryInboxTransportServer> Servers = new();

   public static void BindServerToAddress(EndPointAddress address, MemoryInboxTransportServer server) => Servers[address] = server;

   public static void UnBindAddress(EndPointAddress address) => Servers.TryRemove(address, out _);

   public static MemoryInboxTransportServer GetServer(EndPointAddress address) => Servers[address];
}
