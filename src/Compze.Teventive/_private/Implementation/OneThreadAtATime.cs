using Compze.Contracts;

namespace Compze.Teventive._private.Implementation;

///<summary>Asserts that a taggregate is used by one thread at a time. Publishing, committing and loading enter it, and leave it when it's disposed:<br/>
/// another thread entering meanwhile fails at once. The thread that holds it may enter again, as a tevent handler that publishes from inside a<br/>
/// publish does.</summary>
///<remarks>It makes a taggregate no safer to use from several threads at once: it turns the silent corruption that would cause into a failure.<br/>
/// One thread at a time, not one thread for ever: a taggregate may be used by different threads one after another, as a full rebuild on a thread pool does.<br/>
/// The cost is an int field in the taggregate and one compare-exchange per entry.</remarks>
readonly ref struct OneThreadAtATime
{
   ///<summary>The taggregate's field holding the managed thread id of the thread using it, 0 while none is.</summary>
   readonly ref int _idOfTheThreadUsingIt;
   readonly bool _enteredItFirst;

   OneThreadAtATime(ref int idOfTheThreadUsingIt, bool enteredItFirst)
   {
      _idOfTheThreadUsingIt = ref idOfTheThreadUsingIt;
      _enteredItFirst = enteredItFirst;
   }

   ///<summary>Enters the taggregate whose thread field is <paramref name="idOfTheThreadUsingIt"/> on this thread, or fails if another thread is using it.</summary>
   ///<param name="taggregate">The taggregate entered, for the message when it fails.</param>
   internal static OneThreadAtATime Enter(ref int idOfTheThreadUsingIt, object taggregate)
   {
      var thisThread = Environment.CurrentManagedThreadId;
      var threadUsingIt = Interlocked.CompareExchange(ref idOfTheThreadUsingIt, thisThread, 0);
      if(threadUsingIt != 0 && threadUsingIt != thisThread)
         throw new InvalidOperationException($"A taggregate is used by one thread at a time: thread {thisThread} tried to use the {taggregate.GetType()} that thread {threadUsingIt} is using.");

      return new OneThreadAtATime(ref idOfTheThreadUsingIt, enteredItFirst: threadUsingIt == 0);
   }

   ///<summary>Leaves the taggregate, unless this thread had entered it already when this was entered.</summary>
   public void Dispose()
   {
      if(_enteredItFirst) Volatile.Write(ref _idOfTheThreadUsingIt, 0);
   }
}
