using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.Internal.SqlLayer;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.TessageBus.Private.Inbox;

class InboxTessageStorage(ITessagingSqlLayer.IInboxSqlLayer sqlLayer) : Inbox.ITessageStorage
{
   readonly ITessagingSqlLayer.IInboxSqlLayer _sqlLayer = sqlLayer;

   public async Task<ITessagingSqlLayer.SaveTessageResult> SaveIncomingTessageAsync(TransportTessage.InComing tessage)
      => await _sqlLayer.SaveTessageAsync(tessage.TessageId, tessage.TessageTypeId, tessage.Body).caf();

   public async Task MarkAsSucceededAsync(TransportTessage.InComing tessage)
      => (await _sqlLayer.MarkAsSucceededAsync(tessage.TessageId).caf())
               ._assert(affectedRows => affectedRows == 1);

   public async Task RecordExceptionAsync(TransportTessage.InComing tessage, Exception exception)
   {
      (await _sqlLayer.RecordExceptionAsync(tessage.TessageId,
                                            exception.StackTrace ?? string.Empty,
                                            exception.Message,
                                            exception.GetType().GetFullNameCompilable()).caf())
               ._assert(affectedRows => affectedRows == 1);
   }

   public async Task MarkAsFailedAsync(TransportTessage.InComing tessage) =>
      (await _sqlLayer.MarkAsFailedAsync(tessage.TessageId).caf())
               ._assert(affectedRows => affectedRows == 1);

   public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
}
