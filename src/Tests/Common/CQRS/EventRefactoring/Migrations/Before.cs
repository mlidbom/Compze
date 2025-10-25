using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tests.Common.CQRS.EventRefactoring.Migrations;

public class Before<TEvent> : EventMigration<IRootTevent>
{
   readonly IEnumerable<Type> _insert;

   public static Before<TEvent> Insert<T1>() => new(EnumerableCE.OfTypes<T1>());
   public static Before<TEvent> Insert<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   Before(IEnumerable<Type> insert) : base(Guid.Parse("0533D2E4-DE78-4751-8CAE-3343726D635B"), "Before", "Long description of Before") => _insert = insert;

   public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

   class Inspector(IEnumerable<Type> insert) : ISingleAggregateInstanceHandlingEventMigrator
   {
      readonly IEnumerable<Type> _insert = insert;
      Type? _lastSeenEventType;

      public void MigrateEvent(IAggregateTevent tevent, IEventModifier modifier)
      {
         if (tevent.GetType() == typeof(TEvent) && _lastSeenEventType != _insert.Last())
         {
            modifier.InsertBefore(_insert.Select(Constructor.CreateInstance).Cast<AggregateTevent>().ToArray());
         }

         _lastSeenEventType = tevent.GetType();
      }
   }
}