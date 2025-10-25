using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

class AssertMigrationsAreIdempotentEventModifier : IEventModifier, IStaticInstancePropertySingleton<IEventModifier>
{
   public static IEventModifier Instance { get; } = new AssertMigrationsAreIdempotentEventModifier();
   AssertMigrationsAreIdempotentEventModifier() { }

   public void Replace(params AggregateTevent[] events) => throw new NonIdempotentMigrationDetectedException();

   public void InsertBefore(params AggregateTevent[] insert) => throw new NonIdempotentMigrationDetectedException();
}