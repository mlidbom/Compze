using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;
using Compze.Tessaging.Common;

namespace Compze.Tessaging.Teventive;

public abstract class AggregateWrapperEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateWrapperEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;