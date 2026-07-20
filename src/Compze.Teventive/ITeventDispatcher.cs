using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive;

public interface ITeventDispatcher<in TTevent>
   where TTevent : ITevent
{
   ///<summary>Every tevent is wrapped in an <see cref="IPublisherTevent{TTevent}"/> before routing. This overload wraps <paramref name="evt"/> in a<br/>
   /// <see cref="PublisherTevent{TTevent}"/> closed over its runtime type — a wrapper identifying no publisher — and dispatches the wrapper.</summary>
   void Dispatch(TTevent evt);

   ///<summary>Dispatches a tevent already wrapped in its publisher's <see cref="IPublisherTevent{TTevent}"/>. Routing operates on the wrapper's type:<br/>
   /// subscribers to a wrapper type receive the wrapper itself; subscribers to an inner tevent type receive the unwrapped <see cref="IPublisherTevent{TTevent}.Tevent"/>.</summary>
   void Dispatch(IPublisherTevent<TTevent> evt);
}
