using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
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
