using Compze.Tessaging.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public class VersionSpecification(TessageId teventId, int version)
{
   public TessageId TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}