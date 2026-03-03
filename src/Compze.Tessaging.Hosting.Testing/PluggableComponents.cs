using Compze.Core.Wiring.Testing.Internal;

namespace Compze.Tessaging.Hosting.Testing;

public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer, Serializer Serializer, Transport Transport)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}:{Serializer}:{Transport}";

   public static PluggableComponents FromEnums(IReadOnlyList<Enum> components) => new((SqlLayer)components[0],
                                                                                      (DIContainer)components[1],
                                                                                      (Serializer)components[2],
                                                                                      (Transport)components[3]);
}
