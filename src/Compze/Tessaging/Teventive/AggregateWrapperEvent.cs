using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public abstract class AggregateWrapperTevent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperTevent<TBaseEventInterface>(@event), IAggregateWrapperTevent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateTevent;