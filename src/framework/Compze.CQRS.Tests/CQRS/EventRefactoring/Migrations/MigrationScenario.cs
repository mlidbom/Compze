using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Persistence.EventStore.Refactoring.Migrations;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations;

class MigrationScenario
{
   public readonly IEnumerable<Type> OriginalHistory;
   public readonly IEnumerable<Type> ExpectedHistory;
   public readonly IReadOnlyList<IEventMigration> Migrations;
   public Guid AggregateId { get; }
   static int _instances = 1;

   public MigrationScenario(IEnumerable<Type> originalHistory, IEnumerable<Type> expectedHistory, params IEventMigration[] migrations)
      : this(Guid.Parse($"00000000-0000-0000-0000-0000000{_instances:D5}"),
             originalHistory,
             expectedHistory,
             migrations) {}

   MigrationScenario
   (Guid aggregateId,
    IEnumerable<Type> originalHistory,
    IEnumerable<Type> expectedHistory,
    params IEventMigration[] migrations)
   {
      AggregateId = aggregateId;
      OriginalHistory = originalHistory;
      ExpectedHistory = expectedHistory;
      Migrations = migrations.ToList();
      Interlocked.Increment(ref _instances);
   }
}