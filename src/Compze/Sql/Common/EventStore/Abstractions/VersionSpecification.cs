using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public class VersionSpecification(Guid teventId, int version)
{
   public Guid TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}