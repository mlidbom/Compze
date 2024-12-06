using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Refactoring.Migrations;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ReflectionCE;

namespace Compze.Tests.CQRS.EventRefactoring.Migrations;

class Replace<TEvent> : EventMigration<IRootEvent>
{
   readonly Migrator _migratorSingleton;

   public static Replace<TEvent> With<T1>() => new(EnumerableCE.OfTypes<T1>());
   public static Replace<TEvent> With<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   Replace(IEnumerable<Type> replaceWith) : base(Guid.Parse("9B51F7BC-D9B3-43C7-A183-76CA5E662091"), "Replace", "Long description of Replace") => _migratorSingleton = new Migrator(replaceWith);

   public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => _migratorSingleton;

   class Migrator(IEnumerable<Type> replaceWith) : ISingleAggregateInstanceHandlingEventMigrator
   {
      readonly IEnumerable<Type> _replaceWith = replaceWith;

      public void MigrateEvent(IAggregateEvent @event, IEventModifier modifier)
      {
         if (@event.GetType() == typeof(TEvent))
         {
            modifier.Replace(_replaceWith.Select(Constructor.CreateInstance).Cast<AggregateEvent>().ToArray());
         }
      }
   }
}