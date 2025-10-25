using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Implementation.TessageHandling;

partial class Inbox
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