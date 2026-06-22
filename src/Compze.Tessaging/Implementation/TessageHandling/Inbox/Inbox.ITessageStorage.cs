using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public interface ITessageStorage
   {
      IServiceBusSqlLayer.SaveTessageResult SaveIncomingTessage(TransportTessage.InComing tessage);
      void MarkAsSucceeded(TransportTessage.InComing tessage);
      void RecordException(TransportTessage.InComing tessage, Exception exception );
      void MarkAsFailed(TransportTessage.InComing tessage);
      Task StartAsync();
   }
}