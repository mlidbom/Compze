using Compze.Abstractions.Public;
using Compze.Tessaging;

namespace Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

record struct TaggregateTeventData(TessageId TessageId, int TaggregateVersion, TaggregateId TaggregateId, DateTime UtcTimeStamp);