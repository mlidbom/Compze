using Compze.Abstractions.Wiring.Testing.Internal;

namespace Compze.Tessaging.Hosting.Testing;

public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer, Serializer Serializer, Transport Transport)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}:{Serializer}:{Transport}";
}
