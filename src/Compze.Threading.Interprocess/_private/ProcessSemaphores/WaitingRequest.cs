namespace Compze.Threading.Interprocess._private.ProcessSemaphores;

///<summary>A take of units of an <see cref="IProcessSemaphore"/> that waits in its <see cref="ProcessSemaphoreQueue"/>: how many units, and which<br/>
/// waiter slot its waiter holds while it waits, in whose file the waiter has written what it waits for, which process it is, and since when.</summary>
///<remarks>Public properties, as the queue is kept as JSON.</remarks>
sealed class WaitingRequest
{
   ///<summary>The take's own: two takes of one process are two requests.</summary>
   public Guid Id { get; init; }

   public int Units { get; init; }

   ///<summary>The index of the waiter slot file its waiter holds from before the request is in the queue until after it has left it: free while<br/>
   /// the request is in the queue only when its waiter has died.</summary>
   public int WaiterSlot { get; init; }

   ///<summary>How those who wait after it are told of it: its units, and what its waiter wrote in the waiter slot file of <paramref name="files"/>.</summary>
   internal string Told(ProcessSemaphoreFiles files) => $"{UnitsTold(Units)} {files.WaiterSlot(WaiterSlot).LastHoldersDescription()}";

   internal static string UnitsTold(int units) => units == 1 ? "1 unit" : $"{units} units";
}
