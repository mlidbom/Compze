namespace Compze.Tessaging.TessageBus._private.TessageHandling;

///<summary>The delays between the tries of something that keeps failing, such as a tessage's handling: a few tries at once,<br/>
/// which is what a deadlock needs, then a delay that doubles from 1 second up to 1 minute, so that a failure lasting minutes,<br/>
/// such as a database failover, costs few tries.</summary>
class HandlingRetrySchedule
{
   const int TriesAtOnce = 3;
   static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(1);
   static readonly TimeSpan LongestDelay = TimeSpan.FromMinutes(1);

   int _failuresInARow;

   ///<summary>Counts one more failure in a row, and answers how long to wait before trying again.</summary>
   internal TimeSpan DelayAfterAnotherFailure()
   {
      _failuresInARow++;
      if(_failuresInARow < TriesAtOnce) return TimeSpan.Zero;

      var doublings = Math.Min(_failuresInARow - TriesAtOnce, 10);
      var delay = FirstDelay * (1 << doublings);
      return delay < LongestDelay ? delay : LongestDelay;
   }

   ///<summary>Ends the failures in a row: the next failure is the first of a new row.</summary>
   internal void Succeeded() => _failuresInARow = 0;
}
