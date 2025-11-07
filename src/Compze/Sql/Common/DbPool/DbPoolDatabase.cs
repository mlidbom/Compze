using System;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE;
using JetBrains.Annotations;

namespace Compze.Sql.Common.DbPool;


public class DbPoolDatabase
{
   const string PoolDatabaseNamePrefix = $"Compze_DbPool_";

   public int Id { get; private set; }
   public bool IsReserved { get; private set; }
   public bool IsClean { get; private set; } = true;
   public DateTime ReservationExpirationTime { get; private set; } = DateTime.MinValue;
   public string ReservationName { get; private set; } = string.Empty;
   public DbPoolId? ReservedByPoolId { get; private set; } = null;

   internal string Name => $"{PoolDatabaseNamePrefix}{Id:0000}";

   [UsedImplicitly]public DbPoolDatabase() { }
   internal DbPoolDatabase(int id) => Id = id;
   internal DbPoolDatabase(string name) : this(IdFromName(name)) { }

   internal bool ShouldBeReleased => IsReserved && ReservationExpirationTime < DateTime.UtcNow;

   static int IdFromName(string name)
   {
      var nameIndex = name.ReplaceCE(PoolDatabaseNamePrefix, "");
      return IntCE.ParseInvariant(nameIndex);
   }

   internal DbPoolDatabase Release()
   {
      Assert.State.Is(IsReserved);
      IsReserved = false;
      IsClean = false;
      ReservationName = string.Empty;
      ReservedByPoolId = null;
      return this;
   }

   internal DbPoolDatabase Clean()
   {
      Assert.State.Is(!IsClean);
      IsClean = true;
      return this;
   }

   internal DbPoolDatabase Reserve(string reservationName, DbPoolId poolId, TimeSpan reservationLength)
   {
      Assert.State.Is(!IsReserved);

      IsReserved = true;
      ReservationName = reservationName;
      ReservationExpirationTime = DateTime.UtcNow + reservationLength;
      ReservedByPoolId = poolId;
      return this;
   }

   public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(IsClean)}: {IsClean}, {nameof(ReservationExpirationTime)}: {ReservationExpirationTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
}