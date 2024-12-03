namespace Compze.Messaging.Events;

public interface IEventDispatcher<in TEvent>
{
   void Dispatch(TEvent evt);
}