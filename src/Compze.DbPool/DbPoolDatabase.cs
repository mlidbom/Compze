using Compze.Contracts;
using Compze.Internals.SystemCE;
using JetBrains.Annotations;
using MemoryPack;

namespace Compze.DbPool;

[MemoryPackable]
public partial class DbPoolDatabase
{
   const string PoolDatabaseNamePrefix = "Compze_DbPool_";

   public int Id { get; private set; }
   public bool IsReserved { get; private set; }
   public DateTime ReservationExpirationTime { get; private set; } = DateTime.MinValue;
   public string ReservationName { get; private set; } = string.Empty;
   public Guid? ReservedByPoolId { get; private set; } = null;

   public string Name => $"{PoolDatabaseNamePrefix}{Id:0000}";

   [UsedImplicitly][MemoryPackConstructor]public DbPoolDatabase() { }
   public DbPoolDatabase(int id) => Id = id;
   public DbPoolDatabase(string name) : this(IdFromName(name)) { }

   internal bool ShouldBeReleased => IsReserved && ReservationExpirationTime < DateTime.UtcNow;

   static int IdFromName(string name)
   {
      var nameIndex = name.ReplaceCE(PoolDatabaseNamePrefix, "");
      return IntCE.ParseInvariant(nameIndex);
   }

   internal DbPoolDatabase Release()
   {
      Contract.State.Assert(IsReserved);
      IsReserved = false;
      ReservationName = string.Empty;
      ReservedByPoolId = null;
      return this;
   }

   internal DbPoolDatabase Reserve(string reservationName, Guid poolId, TimeSpan reservationLength)
   {
      Contract.State.Assert(!IsReserved);

      IsReserved = true;
      ReservationName = reservationName;
      ReservationExpirationTime = DateTime.UtcNow + reservationLength;
      ReservedByPoolId = poolId;
      return this;
   }

   public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(ReservationExpirationTime)}: {ReservationExpirationTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
}
