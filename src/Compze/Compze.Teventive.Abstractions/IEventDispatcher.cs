namespace Compze.Teventive.Abstractions;

public interface IEventDispatcher<in TEvent>
{
   void Dispatch(TEvent evt);
}