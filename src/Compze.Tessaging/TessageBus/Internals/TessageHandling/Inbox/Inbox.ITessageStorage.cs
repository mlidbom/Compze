using Compze.Tessaging.Internals.Transport.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.TessageBus.Internals.TessageHandling.Inbox;

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
