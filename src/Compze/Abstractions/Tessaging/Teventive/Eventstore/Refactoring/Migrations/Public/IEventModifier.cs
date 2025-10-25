using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;

public interface IEventModifier
{
   void Replace(params AggregateEvent[] events);
   void InsertBefore(params AggregateEvent[] insert);
}