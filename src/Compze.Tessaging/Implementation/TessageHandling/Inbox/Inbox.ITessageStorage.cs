using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public interface ITessageStorage
   {
      Task<ITessagingSqlLayer.SaveTessageResult> SaveIncomingTessageAsync(TransportTessage.InComing tessage);
      Task MarkAsSucceededAsync(TransportTessage.InComing tessage);
      Task RecordExceptionAsync(TransportTessage.InComing tessage, Exception exception);
      Task MarkAsFailedAsync(TransportTessage.InComing tessage);
      Task StartAsync();
   }
}
