using System.Diagnostics.CodeAnalysis;
using Compze.Internals.SystemCE.LinqCE;
using JetBrains.Annotations;
using MemoryPack;

namespace Compze.DbPool;

[MemoryPackable]
[UsedImplicitly] public partial class DbPoolState
{
   // ReSharper disable once MemberCanBePrivate.Global we'd like serialization to work please
   #pragma warning disable CA1002 //Well this needs to be serializable so the public list is acceptable
   public List<DbPoolDatabase> Databases { get; private set; } = [];
#pragma warning restore CA1002 //Well this needs to be serializable so the public list is acceptable

   IEnumerable<DbPoolDatabase> UnReserved => Databases.Where(db => !db.IsReserved)
                                                       //Reusing recently used databases helps performance in a pretty big way, disk cache, connection pool etc.
                                                      .OrderByDescending(db => db.ReservationExpirationTime);

   public bool TryReserve(string reservationName, Guid poolId, TimeSpan reservationLength, [NotNullWhen(true)] out DbPoolDatabase? reserved)
   {
      CollectGarbage();

      reserved = UnReserved.FirstOrDefault();
      if(reserved == null && Databases.Count < DbPool.NumberOfDatabases)
      {
         Databases.Add(new DbPoolDatabase(Databases.Count + 1));
         reserved = UnReserved.FirstOrDefault();
      }

      reserved?.Reserve(reservationName, poolId, reservationLength);
      return reserved != null;
   }

   void CollectGarbage() => Databases.Where(db => db.ShouldBeReleased)
                                     .ForEach(db => db.Release());

   public void ReleaseReservationsFor(Guid poolId) => DatabasesReservedBy(poolId).ForEach(db => db.Release());

   public DateTime EarliestReservationExpiration => Databases.Where(db => db.IsReserved)
                                                             .Select(db => db.ReservationExpirationTime)
                                                             .DefaultIfEmpty(DateTime.MaxValue)
                                                             .Min();

   IReadOnlyList<DbPoolDatabase> DatabasesReservedBy(Guid poolId) => Databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId).ToList();
}
