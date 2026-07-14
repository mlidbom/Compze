using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public record struct TaggregateTeventData(TessageId TessageId, int TaggregateVersion, TaggregateId TaggregateId, DateTime UtcTimeStamp);