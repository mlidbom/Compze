using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Tests.Common.CQRS.EventRefactoring.Migrations;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tests.Integration.CQRS.EventRefactoring.Migrations;

class Replace<TEvent> : EventMigration<IRootTevent>
{
   readonly Migrator _migratorSingleton;

   public static Replace<TEvent> With<T1>() => new(EnumerableCE.OfTypes<T1>());
   public static Replace<TEvent> With<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   Replace(IEnumerable<Type> replaceWith) : base(Guid.Parse("9B51F7BC-D9B3-43C7-A183-76CA5E662091"), "Replace", "Long description of Replace") => _migratorSingleton = new Migrator(replaceWith);

   public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => _migratorSingleton;

   class Migrator(IEnumerable<Type> replaceWith) : ISingleAggregateInstanceHandlingEventMigrator
   {
      readonly IEnumerable<Type> _replaceWith = replaceWith;

      public void MigrateEvent(IAggregateTevent tevent, IEventModifier modifier)
      {
         if (tevent.GetType() == typeof(TEvent))
         {
            modifier.Replace(_replaceWith.Select(Constructor.CreateInstance).Cast<AggregateTevent>().ToArray());
         }
      }
   }
}