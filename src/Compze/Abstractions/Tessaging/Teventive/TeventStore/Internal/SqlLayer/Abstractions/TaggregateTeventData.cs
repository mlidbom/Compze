using System;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public record struct TaggregateTeventData(TessageId TessageId, int TaggregateVersion, Guid TaggregateId, DateTime UtcTimeStamp)
{
}