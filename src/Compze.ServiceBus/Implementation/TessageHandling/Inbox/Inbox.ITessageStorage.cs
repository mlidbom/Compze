using Compze.ServiceBus.Implementation.Transport.Abstractions;
using Compze.ServiceBus.Transport.SqlLayer;

namespace Compze.ServiceBus.Implementation.TessageHandling.Inbox;

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