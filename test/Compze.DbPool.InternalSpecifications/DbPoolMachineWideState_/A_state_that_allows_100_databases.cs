using Compze.DbPool._private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

namespace Compze.DbPool.InternalSpecifications.DbPoolMachineWideState_;

///<remarks>Internal, because only a state of its own makes it deterministic. Through the public API, its pools would share their reservations<br/>
/// with every other pool on the machine on the same SQL layer, and couldn't reserve every database.</remarks>
public class A_state_that_allows_100_databases : UniversalTestBase
{
   const int MaximumNumberOfDatabases = 100;
   static readonly TimeSpan ReservationLength = TimeSpan.FromSeconds(65);

   readonly AStateOfItsOwn _stateOfItsOwn = new(MaximumNumberOfDatabases);
   DbPoolMachineWideState State => _stateOfItsOwn.State;

   protected override void DisposeInternal() => _stateOfItsOwn.Dispose();

   public class after_one_pool_has_reserved_every_one_under_reservation_names_as_long_as_the_state_has_room_for : A_state_that_allows_100_databases
   {
      ///<summary>A character of the most bytes UTF-8 writes for one, three, which is how the state stores the reservation names: the euro sign.</summary>
      const char ACharacterOfThreeBytesInUtf8 = (char)0x20AC;


      readonly IReadOnlyList<string> _reservedDatabases;

      public after_one_pool_has_reserved_every_one_under_reservation_names_as_long_as_the_state_has_room_for()
      {
         var poolId = Guid.NewGuid();
         _reservedDatabases = [..1.Through(MaximumNumberOfDatabases).Select(number => State.ReserveDatabase(ReservationNameNumbered(number), poolId, ReservationLength).Name)];
      }

      [XF] public void each_reservation_was_given_a_database_of_its_own() => _reservedDatabases.Distinct().Must().HaveCount(MaximumNumberOfDatabases);

      [XF] public void another_pools_reservation_times_out_saying_that_100_of_the_100_databases_were_reserved_by_1_pool_in_1_process_and_that_a_waiting_pool_keeps_the_ones_it_holds() =>
         Invoking(() => State.ReserveDatabase("one more", Guid.NewGuid(), ReservationLength))
           .Must().Throw<TimedOutWaitingForAFreeDatabaseException>()
           .Which.Message.Must().Contain("100 of the 100 databases were reserved, by 1 pool in 1 process")
           .Contain("A pool that waits for a database keeps the ones it holds");

      static string ReservationNameNumbered(int number)
      {
         var prefix = $"{number:000} ";
         return prefix + new string(ACharacterOfThreeBytesInUtf8, DbPoolMachineWideState.LongestReservationNameTheStateHasRoomFor - prefix.Length);
      }
   }
}
