using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

class AssertMigrationsAreIdempotentEventModifier : IEventModifier, IStaticInstancePropertySingleton
{
   public static readonly IEventModifier Instance = new AssertMigrationsAreIdempotentEventModifier();
   AssertMigrationsAreIdempotentEventModifier() { }

   public void Replace(params AggregateEvent[] events) => throw new NonIdempotentMigrationDetectedException();

   public void InsertBefore(params AggregateEvent[] insert) => throw new NonIdempotentMigrationDetectedException();
}