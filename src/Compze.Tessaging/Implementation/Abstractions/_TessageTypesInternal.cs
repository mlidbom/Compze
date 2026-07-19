using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Implementation.Abstractions;

// ReSharper disable once MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
public static class TessageTypesInternal
{
#pragma warning disable CA1040 // Marker interface used for type-routing
   internal interface ITessage : IInternalInfrastructureTessage;
#pragma warning restore CA1040
}
