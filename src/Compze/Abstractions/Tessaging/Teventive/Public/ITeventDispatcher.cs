namespace Compze.Core.Tessaging.Teventive.Public;

public interface ITeventDispatcher<in TTevent>
{
   void Dispatch(TTevent evt);
}