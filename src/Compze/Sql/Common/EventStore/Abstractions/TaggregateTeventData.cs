using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public record struct TaggregateTeventData(Guid TessageId, int TaggregateVersion, Guid TaggregateId, DateTime UtcTimeStamp)
{
}