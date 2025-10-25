using System;

namespace Compze.Sql.Common.TEventStore.Abstractions;

public class VersionSpecification(Guid teventId, int version)
{
   public Guid TeventId { get; } = teventId;
   public int EffectiveVersion { get; } = version;
}