using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Serialization;

public static class TaggregateTeventDebugSerializer
{
   public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{DebugTeventStoreTeventSerializer.Serialize(@this, formatting)}";
}