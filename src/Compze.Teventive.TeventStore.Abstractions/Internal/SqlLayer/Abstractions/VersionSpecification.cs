using Compze.Abstractions.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class VersionSpecification(TessageId teventId, int version)
{
   public TessageId TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}