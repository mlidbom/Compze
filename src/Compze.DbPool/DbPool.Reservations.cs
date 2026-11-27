namespace Compze.DbPool;

public partial class DbPool
{
   ///<summary>The <see cref="Reservation"/>s of a <see cref="DbPool"/>, by reservation name.</summary>
   sealed class Reservations
   {
      readonly Dictionary<string, Reservation> _byName = [];

      ///<summary>The reservation for <paramref name="reservationName"/>, new if the pool has none for it yet.</summary>
      internal Reservation For(string reservationName)
      {
         if(!_byName.TryGetValue(reservationName, out var reservation))
            _byName[reservationName] = reservation = new Reservation();
         return reservation;
      }

      ///<summary>Whether a call is preparing the reservation for <paramref name="reservationName"/>, so that another call for it must wait.</summary>
      internal bool IsBeingPrepared(string reservationName) => _byName.TryGetValue(reservationName, out var reservation) && reservation.IsBeingPrepared;

      ///<summary>Whether a call is preparing any of them.</summary>
      internal bool AnyIsBeingPrepared => _byName.Values.Any(reservation => reservation.IsBeingPrepared);

      ///<summary>The databases reserved for them, reset or not.</summary>
      internal IReadOnlyList<DbPoolDatabase> ReservedDatabases => [.._byName.Values.Select(reservation => reservation.ReservedDatabase).OfType<DbPoolDatabase>()];
   }
}
