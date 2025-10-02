namespace Compze.Tessaging.Teventive;

public interface IEventDispatcher<in TEvent>
{
   void Dispatch(TEvent evt);
}