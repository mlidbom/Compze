using System;
using Compze.Contracts;
using Compze.SystemCE;
using JetBrains.Annotations;

namespace Compze.Testing.Persistence;

partial class DbPool
{
   internal class Database
   {
      public int Id { get; private set; }
      public bool IsReserved { get; private set; }
      public bool IsClean { get; private set; } = true;
      public DateTime ReservationExpirationTime { get; private set; } = DateTime.MinValue;
      public string ReservationName { get; private set; } = string.Empty;
      public Guid ReservedByPoolId { get; private set; } = Guid.Empty;

      internal string Name => $"{PoolDatabaseNamePrefix}{Id:0000}";

      [UsedImplicitly]public Database() { }
      internal Database(int id) => Id = id;
      internal Database(string name) : this(IdFromName(name)) { }

      internal bool ShouldBeReleased => IsReserved && ReservationExpirationTime < DateTime.UtcNow;
      internal bool IsFree => !IsReserved;

      static int IdFromName(string name)
      {
         var nameIndex = name.ReplaceInvariant(PoolDatabaseNamePrefix, "");
         return IntCE.ParseInvariant(nameIndex);
      }

      internal Database Release()
      {
         Contract.Assert.That(IsReserved, "IsReserved");
         IsReserved = false;
         IsClean = false;
         ReservationName = string.Empty;
         ReservedByPoolId = Guid.Empty;
         return this;
      }

      internal Database Clean()
      {
         Contract.Assert.That(!IsClean, "!IsClean");
         IsClean = true;
         return this;
      }

      internal Database Reserve(string reservationName, Guid poolId, TimeSpan reservationLength)
      {
         Contract.Assert.That(!IsReserved, "!IsReserved");
         Contract.Assert.That(poolId != Guid.Empty, "poolId != Guid.Empty");

         IsReserved = true;
         ReservationName = reservationName;
         ReservationExpirationTime = DateTime.UtcNow + reservationLength;
         ReservedByPoolId = poolId;
         return this;
      }

      public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(IsClean)}: {IsClean}, {nameof(ReservationExpirationTime)}: {ReservationExpirationTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
   }
}