namespace Compze.Tessaging.Teventive.Abstractions;

public interface IEventDispatcher<in TEvent>
{
   void Dispatch(TEvent evt);
}