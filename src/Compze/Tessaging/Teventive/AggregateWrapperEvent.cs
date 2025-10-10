using Compze.Tessaging.Common;
using Compze.Tessaging.Teventive.EventStore.Abstractions;

namespace Compze.Tessaging.Teventive;

public abstract class AggregateWrapperEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateWrapperEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;