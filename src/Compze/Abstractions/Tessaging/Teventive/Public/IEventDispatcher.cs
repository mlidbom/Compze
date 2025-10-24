namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface IEventDispatcher<in TEvent>
{
   void Dispatch(TEvent evt);
}