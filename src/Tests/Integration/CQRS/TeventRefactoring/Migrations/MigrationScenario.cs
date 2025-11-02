using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

namespace Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;

class MigrationScenario
{
   public readonly IEnumerable<Type> OriginalHistory;
   public readonly IEnumerable<Type> ExpectedHistory;
   public readonly IReadOnlyList<ITeventMigration> Migrations;
   public TaggregateId TaggregateId { get; }
   static int _instances = 1;

   public MigrationScenario(IEnumerable<Type> originalHistory, IEnumerable<Type> expectedHistory, params ITeventMigration[] migrations)
      : this(new TaggregateId(Guid.Parse($"00000000-0000-0000-0000-0000000{_instances:D5}")),
             originalHistory,
             expectedHistory,
             migrations) {}

   MigrationScenario
   (TaggregateId taggregateId,
    IEnumerable<Type> originalHistory,
    IEnumerable<Type> expectedHistory,
    params ITeventMigration[] migrations)
   {
      TaggregateId = taggregateId;
      OriginalHistory = originalHistory;
      ExpectedHistory = expectedHistory;
      Migrations = migrations.ToList();
      Interlocked.Increment(ref _instances);
   }
}
