using System.Globalization;

namespace Compze.DbPool._private;

///<summary>Thrown by <see cref="DbPool.Dispose"/> when the pool held a database past the end of its reservation. Once a reservation has ended,<br/>
/// another pool may reserve the database and reset it while it is still in use, which fails its users in ways that point elsewhere, such as an<br/>
/// unknown database or a missing table. So a pool that held one that long fails loudly, whether or not another pool reserved it meanwhile.</summary>
sealed class DatabaseHeldPastItsReservationException : Exception
{
   internal DatabaseHeldPastItsReservationException(IReadOnlyList<DbPoolDatabase> databasesHeldPastTheirReservation, TimeSpan reservationLength, DateTime disposedAt)
      : base(MessageFor(databasesHeldPastTheirReservation, reservationLength, disposedAt)) {}

   static string MessageFor(IReadOnlyList<DbPoolDatabase> databasesHeldPastTheirReservation, TimeSpan reservationLength, DateTime disposedAt)
   {
      var databases = string.Join(", ", databasesHeldPastTheirReservation.Select(DescriptionOf));
      return $"""
              This DbPool held databases past the end of their reservations of {reservationLength.TotalSeconds.ToString(CultureInfo.InvariantCulture)} seconds: {databases}.
              Once a reservation has ended, another pool may reserve the database and reset it while it is still in use, which fails its users in ways that point elsewhere, such as an unknown database or a missing table.
              """;

      string DescriptionOf(DbPoolDatabase database)
      {
         var heldFor = disposedAt - (database.ReservationExpirationTime - reservationLength);
         return $"{database.Name}, reserved as '{database.ReservationName}', held for {heldFor.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds";
      }
   }
}
