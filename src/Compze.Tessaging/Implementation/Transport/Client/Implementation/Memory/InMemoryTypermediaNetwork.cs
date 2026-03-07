using System.Collections.Concurrent;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Typermedia.Hosting;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class InMemoryTypermediaNetwork
{
   static readonly ConcurrentDictionary<EndPointAddress, TypermediaHandlerExecutor> Executors = new();

   public static void BindExecutor(EndPointAddress address, TypermediaHandlerExecutor executor) => Executors[address] = executor;

   public static void UnBind(EndPointAddress address) => Executors.TryRemove(address, out _);

   public static TypermediaHandlerExecutor GetExecutor(EndPointAddress address) => Executors[address];
}
