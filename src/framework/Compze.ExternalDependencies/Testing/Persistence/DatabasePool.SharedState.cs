using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.SystemCE.LinqCE;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Testing.Persistence;

partial class DbPool
{
   [UsedImplicitly] protected class SharedState
   {
      const int CleanDatabaseNumberTarget = 10;
      [JsonProperty]
      List<Database> _databases = [];

      IEnumerable<Database> UnReserved => _databases.Where(db => !db.IsReserved)
                                                     //Reusing recently used databases helps performance in a pretty big way, disk cache, connection pool etc.
                                                    .OrderByDescending(db => db.ReservationExpirationTime);

      IEnumerable<Database> DirtyUnReserved => UnReserved.Where(db => !db.IsClean);

      IEnumerable<Database> CleanUnReserved => UnReserved.Where(db => db.IsClean);

      internal bool TryReserve(string reservationName, Guid poolId, TimeSpan reservationLength, [NotNullWhen(true)] out Database? reserved)
      {
         CollectGarbage();

         reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
         if(reserved == null && _databases.Count < NumberOfDatabases)
         {
            _databases.Add(new Database(_databases.Count + 1));
            reserved = CleanUnReserved.FirstOrDefault() ?? UnReserved.FirstOrDefault();
         }

         reserved?.Reserve(reservationName, poolId, reservationLength);
         return reserved != null;
      }

      void CollectGarbage() => _databases.Where(db => db.ShouldBeReleased)
                                         .ForEach(db => db.Release());

      internal void ReleaseReservationsFor(Guid poolId) => DatabasesReservedBy(poolId).ForEach(db => db.Release());

      IReadOnlyList<Database> DatabasesReservedBy(Guid poolId) => _databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId).ToList();
   }
}