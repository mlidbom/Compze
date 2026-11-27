using Compze.Contracts;
using Compze.DbPool._private;
using Compze.Internals.SystemCE;
using JetBrains.Annotations;
using MemoryPack;

namespace Compze.DbPool;

[MemoryPackable]
public partial class DbPoolDatabase
{
   const string PoolDatabaseNamePrefix = $"Compze_DbPool_{DbPoolMachineWideState.FormatVersion}_";

   public int Id { get; private set; }
   public bool IsReserved { get; private set; }
   public DateTime ReservationExpirationTime { get; private set; } = DateTime.MinValue;
   public string ReservationName { get; private set; } = string.Empty;
   public Guid? ReservedByPoolId { get; private set; } = null;

   ///<summary>The process this database is reserved in, while it is reserved.</summary>
   [MemoryPackInclude] internal ReservingProcess? ReservedInProcess { get; private set; }

   public string Name => $"{PoolDatabaseNamePrefix}{Id:0000}";

   [UsedImplicitly][MemoryPackConstructor]public DbPoolDatabase() { }
   public DbPoolDatabase(int id) => Id = id;
   public DbPoolDatabase(string name) : this(IdFromName(name)) { }

   internal bool ReservationHasExpired => ReservationExpirationTime < DateTime.UtcNow;

   static int IdFromName(string name)
   {
      var nameIndex = name.ReplaceOrdinal(PoolDatabaseNamePrefix, "");
      return IntCE.ParseInvariant(nameIndex);
   }

   internal DbPoolDatabase Release()
   {
      Contract.State.Assert(IsReserved);
      IsReserved = false;
      ReservationName = string.Empty;
      ReservedByPoolId = null;
      ReservedInProcess = null;
      return this;
   }

   internal DbPoolDatabase Reserve(string reservationName, Guid poolId, TimeSpan reservationLength)
   {
      Contract.State.Assert(!IsReserved);

      IsReserved = true;
      ReservationName = reservationName;
      ReservationExpirationTime = DateTime.UtcNow + reservationLength;
      ReservedByPoolId = poolId;
      ReservedInProcess = ReservingProcess.Current;
      return this;
   }

   public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(ReservationExpirationTime)}: {ReservationExpirationTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}, {nameof(ReservedInProcess)}:{ReservedInProcess}";
}
