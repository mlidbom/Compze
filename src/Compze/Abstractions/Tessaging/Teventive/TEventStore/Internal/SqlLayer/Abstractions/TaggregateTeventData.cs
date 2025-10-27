using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public record struct TaggregateTeventData(Guid TessageId, int TaggregateVersion, Guid TaggregateId, DateTime UtcTimeStamp)
{
}