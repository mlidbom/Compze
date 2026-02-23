using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public class InboxTessageStorage(IServiceBusSqlLayer.IInboxSqlLayer sqlLayer) : Inbox.ITessageStorage
{
   readonly IServiceBusSqlLayer.IInboxSqlLayer _sqlLayer = sqlLayer;

   public IServiceBusSqlLayer.SaveTessageResult SaveIncomingTessage(TransportTessage.InComing tessage)
      => _sqlLayer.SaveTessage(tessage.TessageId, tessage.TessageTypeId, tessage.Body);

   public void MarkAsSucceeded(TransportTessage.InComing tessage)
      => _sqlLayer.MarkAsSucceeded(tessage.TessageId);

   public void RecordException(TransportTessage.InComing tessage, Exception exception)
   {
      var affectedRows = _sqlLayer.RecordException(tessage.TessageId,
                                                           exception.StackTrace ?? string.Empty,
                                                           exception.Message,
                                                           exception.GetType().GetFullNameCompilable());

      Assert.ReturnValue.Is(affectedRows == 1);
   }

   public void MarkAsFailed(TransportTessage.InComing tessage)
   {
      var affectedRows = _sqlLayer.MarkAsFailed(tessage.TessageId);
      Assert.ReturnValue.Is(affectedRows == 1);
   }

   public Task StartAsync() => _sqlLayer.InitAsync();
}