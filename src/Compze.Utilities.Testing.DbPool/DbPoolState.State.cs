using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Utilities.SystemCE.LinqCE;
using JetBrains.Annotations;

namespace Compze.Utilities.Testing.DbPool;

[UsedImplicitly] public class DbPoolState
{
   // ReSharper disable once MemberCanBePrivate.Global we'd like serialization to work please
   #pragma warning disable CA1002 //Well this needs to be serializable so the public list is acceptable
   public List<DbPoolDatabase> Databases { get; private set; } = [];
#pragma warning restore CA1002 //Well this needs to be serializable so the public list is acceptable

   IEnumerable<DbPoolDatabase> UnReserved => Databases.Where(db => !db.IsReserved)
                                                       //Reusing recently used databases helps performance in a pretty big way, disk cache, connection pool etc.
                                                      .OrderByDescending(db => db.ReservationExpirationTime);

   IEnumerable<DbPoolDatabase> CleanUnReserved => UnReserved.Where(db => db.IsClean);

   public bool TryReserve(string reservationName, Guid poolId, TimeSpan reservationLength, [NotNullWhen(true)] out DbPoolDatabase? reserved)
   {
      CollectGarbage();

      reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
      if(reserved == null && Databases.Count < DbPool.NumberOfDatabases)
      {
         Databases.Add(new DbPoolDatabase(Databases.Count + 1));
         reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
      }

      reserved?.Reserve(reservationName, poolId, reservationLength);
      return reserved != null;
   }

   void CollectGarbage() => Databases.Where(db => db.ShouldBeReleased)
                                     .ForEach(db => db.Release());

   public void ReleaseReservationsFor(Guid poolId) => DatabasesReservedBy(poolId).ForEach(db => db.Release());

   IReadOnlyList<DbPoolDatabase> DatabasesReservedBy(Guid poolId) => Databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId).ToList();
}
