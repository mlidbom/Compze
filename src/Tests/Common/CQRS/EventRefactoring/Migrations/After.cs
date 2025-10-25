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

public class After<TEvent> : EventMigration<IRootTevent>
{
   readonly IEnumerable<Type> _insert;

   public static After<TEvent> Insert<T1>() => new(EnumerableCE.OfTypes<T1>());
   // ReSharper disable once UnusedMember.Global todo:Write test that uses this. We should have a test replacing with a collection.
   public static After<TEvent> Insert<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   After(IEnumerable<Type> insert) : base(Guid.Parse("544C6694-7B29-4CC0-8DAA-6C50A5F28B70"), "After", "Long description of After") => _insert = insert;

   public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

   class Inspector(IEnumerable<Type> insert) : ISingleAggregateInstanceHandlingEventMigrator
   {
      readonly IEnumerable<Type> _insert = insert;
      Type? _lastSeenEventType;

      public void MigrateEvent(IAggregateTevent tevent, IEventModifier modifier)
      {
         if (_lastSeenEventType == typeof(TEvent) && tevent.GetType() != _insert.First())
         {
            modifier.InsertBefore(_insert.Select(Constructor.CreateInstance).Cast<AggregateTevent>().ToArray());
         }

         _lastSeenEventType = tevent.GetType();
      }

   }
}