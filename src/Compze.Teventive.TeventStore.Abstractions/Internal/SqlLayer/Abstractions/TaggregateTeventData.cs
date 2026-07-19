using Compze.Abstractions.Public;
using Compze.Tessaging.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public record struct TaggregateTeventData(TessageId TessageId, int TaggregateVersion, TaggregateId TaggregateId, DateTime UtcTimeStamp);