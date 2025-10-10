using Compze.Tessaging.Teventive.EventStore.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

public interface IEventModifier
{
   void Replace(params AggregateEvent[] events);
   void InsertBefore(params AggregateEvent[] insert);
}