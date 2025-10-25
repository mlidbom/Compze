using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class VersionSpecification(Guid teventId, int version)
{
   public Guid TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}