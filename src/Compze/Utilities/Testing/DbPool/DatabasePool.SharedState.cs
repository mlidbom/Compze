using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Utilities.SystemCE.LinqCE;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.DbPool;

public partial class DbPoolBase
{
   [UsedImplicitly] protected class SharedState
   {
      [JsonProperty]
      List<DbPoolDatabase> _databases = [];

      IEnumerable<DbPoolDatabase> UnReserved => _databases.Where(db => !db.IsReserved)
                                                     //Reusing recently used databases helps performance in a pretty big way, disk cache, connection pool etc.
                                                    .OrderByDescending(db => db.ReservationExpirationTime);

      IEnumerable<DbPoolDatabase> CleanUnReserved => UnReserved.Where(db => db.IsClean);

      internal bool TryReserve(string reservationName, Guid poolId, TimeSpan reservationLength, [NotNullWhen(true)] out DbPoolDatabase? reserved)
      {
         CollectGarbage();

         reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
         if(reserved == null && _databases.Count < NumberOfDatabases)
         {
            _databases.Add(new DbPoolDatabase(_databases.Count + 1));
            reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
         }

         reserved?.Reserve(reservationName, poolId, reservationLength);
         return reserved != null;
      }

      void CollectGarbage() => _databases.Where(db => db.ShouldBeReleased)
                                         .ForEach(db => db.Release());

      internal void ReleaseReservationsFor(Guid poolId) => DatabasesReservedBy(poolId).ForEach(db => db.Release());

      IReadOnlyList<DbPoolDatabase> DatabasesReservedBy(Guid poolId) => _databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId).ToList();
   }
}