using System.Diagnostics;
using MemoryPack;

namespace Compze.DbPool._private;

///<summary>The process a <see cref="DbPoolDatabase"/> is reserved in, recorded with its reservation, so that the next reservation, by any pool on<br/>
/// the machine, can release the reservations of a process that has exited: a process that is killed or crashes never disposes its pools.</summary>
///<remarks>A process is known by its id alone. When the operating system gives the id of a process that has exited to a new one, the reservations<br/>
/// of the exited process look held, and end only when they expire. So a reused id costs a wait, never a reset of a database still in use.</remarks>
[MemoryPackable]
sealed partial class ReservingProcess : IEquatable<ReservingProcess>
{
   [MemoryPackInclude] int Id { get; }

   [MemoryPackConstructor] ReservingProcess(int id) => Id = id;

   ///<summary>The process this code runs in.</summary>
   internal static ReservingProcess Current { get; } = new(Environment.ProcessId);

   internal bool HasExited
   {
      get
      {
         try
         {
            using var process = Process.GetProcessById(Id);
            return false;
         }
         catch(ArgumentException) //What Process.GetProcessById throws when no process with the id runs.
         {
            return true;
         }
      }
   }

   public bool Equals(ReservingProcess? other) => other is not null && other.Id == Id;
   public override bool Equals(object? obj) => obj is ReservingProcess other && Equals(other);
   public override int GetHashCode() => Id;
   public override string ToString() => $"process {Id}";
}
