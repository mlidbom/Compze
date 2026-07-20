using Compze.Tessaging;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

class VersionSpecification(TessageId teventId, int version)
{
   public TessageId TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}