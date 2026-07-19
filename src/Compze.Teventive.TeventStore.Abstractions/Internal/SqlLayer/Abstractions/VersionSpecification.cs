using Compze.Abstractions.Public;
using Compze.Tessaging.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public class VersionSpecification(TessageId teventId, int version)
{
   public TessageId TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}