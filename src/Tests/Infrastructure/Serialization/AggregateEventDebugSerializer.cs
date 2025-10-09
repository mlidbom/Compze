using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Serialization;

public static class AggregateEventDebugSerializer
{
   public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{DebugEventStoreEventSerializer.Serialize(@this, formatting)}";
}