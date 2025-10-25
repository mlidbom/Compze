using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public record struct AggregateTeventData(Guid TessageId, int AggregateVersion, Guid AggregateId, DateTime UtcTimeStamp)
{
}