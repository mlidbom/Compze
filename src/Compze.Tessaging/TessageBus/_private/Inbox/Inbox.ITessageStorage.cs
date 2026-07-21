using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   internal interface ITessageStorage
   {
      Task<ITessagingSqlLayer.SaveTessageResult> SaveIncomingTessageAsync(TransportTessage.InComing tessage);

      ///<summary>Claims the tessage for the caller's handling execution, exclusively, riding its ambient handling<br/>
      /// transaction — see <see cref="ITessagingSqlLayer.IInboxSqlLayer.TryClaimForHandlingAsync"/>. False means the tessage<br/>
      /// is not this execution's to handle, and the caller skips without touching it.</summary>
      Task<bool> TryClaimForHandlingAsync(TransportTessage.InComing tessage);

      Task MarkAsSucceededAsync(TransportTessage.InComing tessage);
      Task RecordExceptionAsync(TransportTessage.InComing tessage, Exception exception);
      Task MarkAsFailedAsync(TransportTessage.InComing tessage);
      Task StartAsync();
   }
}
