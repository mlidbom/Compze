namespace Compze.Abstractions.Tessaging.Public;

///<summary>Publishes tevents from code that runs outside any unit of work — application code with no ambient scope or<br/>
/// transaction, such as host-level infrastructure or a service narrating analytics facts. Each publish runs as its own<br/>
/// independent unit of work: a fresh scope paired with its own transaction, committed when the call returns. The independent<br/>
/// counterpart of <see cref="IUnitOfWorkTeventPublisher"/>, which publishes within the caller's unit of work.</summary>
///<remarks>Why it exists: <see cref="IUnitOfWorkTeventPublisher"/> is scoped — participation delivers to handlers through the<br/>
/// caller's scope — so code outside any scope cannot resolve it. Without this door such code must hand-build a unit of work out<br/>
/// of container primitives to say one domain verb. This publisher is a singleton, resolvable from the root, so a plain<br/>
/// application class takes it as an ordinary constructor dependency and publishes with one call.</remarks>
///<remarks>Independence is asserted, not assumed: publishing from within an ambient transaction throws, because the publish<br/>
/// would silently join that transaction instead of standing alone. Inside a unit of work, publish through<br/>
/// <see cref="IUnitOfWorkTeventPublisher"/>, which deliberately joins it.</remarks>
public interface IIndependentTeventPublisher
{
   ///<summary>Publishes <paramref name="tevent"/> as its own unit of work, routed per the delivery contract its type declares —<br/>
   /// see <see cref="IUnitOfWorkTeventPublisher.Publish"/>. The unit of work commits when the call returns: an<br/>
   /// <see cref="IExactlyOnceTevent"/> is then durably on its way, a best-effort remotable tevent has been handed to the wire,<br/>
   /// and this process's subscribed handlers have already run.</summary>
   void Publish(ITevent tevent);
}
