using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;

public interface IEventModifier
{
   void Replace(params AggregateTevent[] events);
   void InsertBefore(params AggregateTevent[] insert);
}