using Compze.Tessaging.Internal.Transport.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.TessageBus.Internal.Inbox;

public partial class Inbox
{
   internal interface ITessageStorage
   {
      Task<ITessagingSqlLayer.SaveTessageResult> SaveIncomingTessageAsync(TransportTessage.InComing tessage);
      Task MarkAsSucceededAsync(TransportTessage.InComing tessage);
      Task RecordExceptionAsync(TransportTessage.InComing tessage, Exception exception);
      Task MarkAsFailedAsync(TransportTessage.InComing tessage);
      Task StartAsync();
   }
}
