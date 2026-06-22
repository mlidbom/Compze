using Compze.Abstractions.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public record struct TaggregateTeventData(TessageId TessageId, int TaggregateVersion, TaggregateId TaggregateId, DateTime UtcTimeStamp);