using System;
using System.Collections.Generic;
using Compze.Core.Wiring.Testing.Internal;

namespace Compze.Tessaging.Hosting.Testing;

public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}";

   public static PluggableComponents FromEnums(IReadOnlyList<Enum> parts) => new((SqlLayer)parts[0], (DIContainer)parts[1]);
}
